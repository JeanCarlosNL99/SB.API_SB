using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Domain.ValueObjects;
using SB.API_SB.Services.Payroll;
using SB.API_SB.Tests.TestDoubles;
using Xunit;

namespace SB.API_SB.Tests.Services;

/// <summary>
/// Pruebas del calculo de pagos semanales.
/// </summary>
/// <remarks>
/// La regla que se verifica con mas insistencia es que una semana no se pueda
/// pagar dos veces: es la que protege a la entidad gubernamental de un pago duplicado, y la
/// que el enunciado exige explicitamente.
/// </remarks>
public sealed class PayrollRunServiceTests
{
    private const int PERFORMANCE_EMPLOYEE_COUNT = 1_000;
    private const int PERFORMANCE_LIMIT_IN_MILLISECONDS = 2_000;

    private static readonly Guid GOVERNMENT_ENTITY_ID = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime FIXED_NOW = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly IPayrollRunRepository payrollRunRepository =
        Substitute.For<IPayrollRunRepository>();

    private readonly IGovernmentEntityRepository governmentEntityRepository =
        Substitute.For<IGovernmentEntityRepository>();
    private readonly IEmployeeRepository employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PayrollRunService payrollRunService;

    /// <summary>Semana ya terminada, valida para generar nomina.</summary>
    private readonly PayrollWeek pastWeek = PayrollWeek.Current(FIXED_NOW).Previous();

    public PayrollRunServiceTests()
    {
        governmentEntityRepository
            .GetByIdAsync(GOVERNMENT_ENTITY_ID, Arg.Any<CancellationToken>())
            .Returns(new GovernmentEntity
            {
                Id = GOVERNMENT_ENTITY_ID,
                Name = "Direccion General de Impuestos Internos",
                Category = "Organismo Descentralizado Funcionalmente",
                StateBranch = "Poder Ejecutivo",
                Sector = "Hacienda",
                Status = RecordStatus.Active
            });

        employeeRepository
            .GetForPayrollAsync(GOVERNMENT_ENTITY_ID, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(BuildSampleEmployees());

        payrollRunService = new PayrollRunService(
            payrollRunRepository,
            governmentEntityRepository,
            employeeRepository,
            new PayrollCalculator(EmployeeTypeHandlerResolverFactory.Create()),
            new FixedDateTimeProvider(FIXED_NOW),
            unitOfWork,
            NullLogger<PayrollRunService>.Instance);
    }

    [Fact]
    public async Task GenerateAsync_SemanaLibre_PersisteLaNominaConSusLineasYTotales()
    {
        CapturedPayrollRun generatedRun = CaptureGeneratedRun();

        PayrollRunDetailResponse response = await payrollRunService.GenerateAsync(
            BuildRequest());

        Assert.NotNull(generatedRun.Value);
        Assert.Equal(GOVERNMENT_ENTITY_ID, generatedRun.Value!.GovernmentEntityId);
        Assert.Equal(pastWeek.Year, generatedRun.Value!.Year);
        Assert.Equal(pastWeek.WeekNumber, generatedRun.Value!.WeekNumber);
        Assert.Equal(pastWeek.StartDate, generatedRun.Value!.WeekStartDate);
        Assert.Equal(pastWeek.EndDate, generatedRun.Value!.WeekEndDate);
        Assert.Equal(PayrollRunStatus.Generated, generatedRun.Value!.Status);

        // 35,000 asalariado + 22,050 por horas + 20,000 por comision + 31,000
        // asalariado por comision.
        Assert.Equal(4, generatedRun.Value!.EmployeeCount);
        Assert.Equal(108_050m, generatedRun.Value!.TotalAmount);
        Assert.Equal(108_050m, response.Summary.TotalAmount);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SemanaYaGenerada_LanzaExcepcionYNoPersiste()
    {
        Guid existingRunId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        payrollRunRepository
            .FindGeneratedRunAsync(GOVERNMENT_ENTITY_ID, Arg.Any<PayrollWeek>(), Arg.Any<CancellationToken>())
            .Returns(new PayrollRun
            {
                Id = existingRunId,
                GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                Year = pastWeek.Year,
                WeekNumber = pastWeek.WeekNumber,
                Status = PayrollRunStatus.Generated
            });

        DuplicatedPayrollRunException exception =
            await Assert.ThrowsAsync<DuplicatedPayrollRunException>(
                () => payrollRunService.GenerateAsync(BuildRequest()));

        Assert.Equal(existingRunId, exception.ExistingPayrollRunId);
        Assert.Equal(pastWeek.Year, exception.Year);
        Assert.Equal(pastWeek.WeekNumber, exception.WeekNumber);
        Assert.Equal("NOMINA_SEMANA_YA_GENERADA", exception.ErrorCode);

        await payrollRunRepository.DidNotReceive().AddAsync(
            Arg.Any<PayrollRun>(),
            Arg.Any<CancellationToken>());

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SemanaQueTodaviaNoComienza_LanzaExcepcionDeReglaDeNegocio()
    {
        PayrollWeek futureWeek = PayrollWeek.Current(FIXED_NOW).Next().Next();

        GeneratePayrollRunRequest request = new()
        {
            GovernmentEntityId = GOVERNMENT_ENTITY_ID,
            Year = futureWeek.Year,
            WeekNumber = futureWeek.WeekNumber
        };

        BusinessRuleViolationException exception =
            await Assert.ThrowsAsync<BusinessRuleViolationException>(
                () => payrollRunService.GenerateAsync(request));

        Assert.Contains("todavia no ha comenzado", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_EntidadGubernamentalSinEmpleados_LanzaExcepcionDeReglaDeNegocio()
    {
        employeeRepository
            .GetForPayrollAsync(GOVERNMENT_ENTITY_ID, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());

        BusinessRuleViolationException exception =
            await Assert.ThrowsAsync<BusinessRuleViolationException>(
                () => payrollRunService.GenerateAsync(BuildRequest()));

        Assert.Contains("no tiene empleados", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_EntidadGubernamentalInactiva_LanzaExcepcionDeReglaDeNegocio()
    {
        governmentEntityRepository
            .GetByIdAsync(GOVERNMENT_ENTITY_ID, Arg.Any<CancellationToken>())
            .Returns(new GovernmentEntity
            {
                Id = GOVERNMENT_ENTITY_ID,
                Name = "Entidad Suprimida",
                Status = RecordStatus.Inactive
            });

        BusinessRuleViolationException exception =
            await Assert.ThrowsAsync<BusinessRuleViolationException>(
                () => payrollRunService.GenerateAsync(BuildRequest()));

        Assert.Contains("inactiva", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_EntidadGubernamentalInexistente_LanzaExcepcionDeNoEncontrado()
    {
        Guid unknownGovernmentEntityId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        governmentEntityRepository
            .GetByIdAsync(unknownGovernmentEntityId, Arg.Any<CancellationToken>())
            .Returns((GovernmentEntity?)null);

        GeneratePayrollRunRequest request = BuildRequest();
        request.GovernmentEntityId = unknownGovernmentEntityId;

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => payrollRunService.GenerateAsync(request));
    }

    [Fact]
    public async Task GenerateAsync_ConservaLaFormulaYElDesgloseDeCadaLinea()
    {
        CapturedPayrollRun generatedRun = CaptureGeneratedRun();

        await payrollRunService.GenerateAsync(BuildRequest());

        PayrollRunLine hourlyLine = generatedRun.Value!.Lines
            .Single(line => line.EmployeeType == EmployeeType.Hourly);

        // La instantanea guarda la formula y los componentes: el historico se puede
        // auditar sin volver a calcular nada.
        Assert.Contains("sueldoPorHora", hourlyLine.PaymentFormula);
        Assert.Equal(2, hourlyLine.Components.Count);
        Assert.Equal(
            hourlyLine.WeeklyPayment,
            hourlyLine.Components.Sum(component => component.Amount));
        Assert.Contains(hourlyLine.Components, component => component.Concept == "Horas extras");
    }

    [Fact]
    public async Task PreviewAsync_SemanaYaGenerada_LoInformaSinLanzarExcepcion()
    {
        Guid existingRunId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        payrollRunRepository
            .FindGeneratedRunAsync(GOVERNMENT_ENTITY_ID, Arg.Any<PayrollWeek>(), Arg.Any<CancellationToken>())
            .Returns(new PayrollRun { Id = existingRunId, GovernmentEntityId = GOVERNMENT_ENTITY_ID });

        PayrollPreviewResponse preview = await payrollRunService.PreviewAsync(
            GOVERNMENT_ENTITY_ID,
            pastWeek.Year,
            pastWeek.WeekNumber,
            onlyActiveEmployees: true);

        Assert.True(preview.IsAlreadyGenerated);
        Assert.Equal(existingRunId, preview.ExistingPayrollRunId);
        Assert.Equal(4, preview.EmployeeCount);
        Assert.Equal(108_050m, preview.TotalAmount);
    }

    [Fact]
    public async Task PreviewAsync_NoPersisteNada()
    {
        await payrollRunService.PreviewAsync(
            GOVERNMENT_ENTITY_ID,
            pastWeek.Year,
            pastWeek.WeekNumber,
            onlyActiveEmployees: true);

        await payrollRunRepository.DidNotReceive().AddAsync(
            Arg.Any<PayrollRun>(),
            Arg.Any<CancellationToken>());

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewAsync_YGenerateAsync_ProducenExactamenteElMismoCalculo()
    {
        CapturedPayrollRun generatedRun = CaptureGeneratedRun();

        PayrollPreviewResponse preview = await payrollRunService.PreviewAsync(
            GOVERNMENT_ENTITY_ID,
            pastWeek.Year,
            pastWeek.WeekNumber,
            onlyActiveEmployees: true);

        await payrollRunService.GenerateAsync(BuildRequest());

        // Lo que el usuario revisa antes de generar debe ser identico a lo que queda
        // almacenado: ambas rutas comparten el mismo calculador.
        Assert.Equal(preview.EmployeeCount, generatedRun.Value!.EmployeeCount);
        Assert.Equal(preview.TotalAmount, generatedRun.Value!.TotalAmount);
        Assert.Equal(
            preview.Lines.Select(line => line.WeeklyPayment).OrderBy(amount => amount),
            generatedRun.Value!.Lines.Select(line => line.WeeklyPayment).OrderBy(amount => amount));
    }

    [Fact]
    public async Task CancelAsync_MarcaLaEjecucionComoAnuladaYRegistraElMotivo()
    {
        const string CANCELLATION_REASON = "Se cargaron horas incorrectas en el periodo.";
        Guid payrollRunId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        PayrollRun existingRun = new()
        {
            Id = payrollRunId,
            GovernmentEntityId = GOVERNMENT_ENTITY_ID,
            Year = pastWeek.Year,
            WeekNumber = pastWeek.WeekNumber,
            Status = PayrollRunStatus.Generated
        };

        payrollRunRepository
            .GetWithDetailAsync(payrollRunId, Arg.Any<CancellationToken>())
            .Returns(existingRun);

        await payrollRunService.CancelAsync(
            payrollRunId,
            new CancelPayrollRunRequest { Reason = CANCELLATION_REASON });

        Assert.Equal(PayrollRunStatus.Cancelled, existingRun.Status);
        Assert.Equal(CANCELLATION_REASON, existingRun.CancellationReason);
        Assert.Equal(FIXED_NOW, existingRun.CancelledAt);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_EjecucionYaAnulada_LanzaExcepcionDeReglaDeNegocio()
    {
        Guid payrollRunId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        payrollRunRepository
            .GetWithDetailAsync(payrollRunId, Arg.Any<CancellationToken>())
            .Returns(new PayrollRun
            {
                Id = payrollRunId,
                GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                Status = PayrollRunStatus.Cancelled
            });

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => payrollRunService.CancelAsync(
                payrollRunId,
                new CancelPayrollRunRequest { Reason = "Motivo suficientemente largo." }));
    }

    [Fact]
    public async Task SearchAsync_TrasladaLosFiltrosRecibidosAlRepositorio()
    {
        payrollRunRepository
            .SearchAsync(Arg.Any<PayrollRunFilterCriteria>(), Arg.Any<CancellationToken>())
            .Returns(PagedList<PayrollRun>.Empty(pageNumber: 1, pageSize: 10));

        PayrollRunFilterRequest filter = new()
        {
            GovernmentEntityId = GOVERNMENT_ENTITY_ID,
            Year = 2026,
            IncludeCancelled = false,
            PageNumber = 3,
            PageSize = 25
        };

        await payrollRunService.SearchAsync(filter);

        await payrollRunRepository.Received(1).SearchAsync(
            Arg.Is<PayrollRunFilterCriteria>(criteria =>
                criteria.GovernmentEntityId == GOVERNMENT_ENTITY_ID &&
                criteria.Year == 2026 &&
                !criteria.IncludeCancelled &&
                criteria.PageNumber == 3 &&
                criteria.PageSize == 25),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifica el requisito no funcional: procesar los calculos de hasta 1,000
    /// empleados en menos de 2 segundos.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ConMilEmpleados_TerminaEnMenosDeDosSegundos()
    {
        employeeRepository
            .GetForPayrollAsync(GOVERNMENT_ENTITY_ID, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(BuildManyEmployees(PERFORMANCE_EMPLOYEE_COUNT));

        CapturedPayrollRun generatedRun = CaptureGeneratedRun();

        Stopwatch elapsedTimeWatch = Stopwatch.StartNew();

        await payrollRunService.GenerateAsync(BuildRequest());

        elapsedTimeWatch.Stop();

        Assert.Equal(PERFORMANCE_EMPLOYEE_COUNT, generatedRun.Value!.EmployeeCount);
        Assert.True(
            elapsedTimeWatch.ElapsedMilliseconds < PERFORMANCE_LIMIT_IN_MILLISECONDS,
            $"La nomina de {PERFORMANCE_EMPLOYEE_COUNT} empleados tardo " +
            $"{elapsedTimeWatch.ElapsedMilliseconds} ms, por encima del limite de " +
            $"{PERFORMANCE_LIMIT_IN_MILLISECONDS} ms.");
    }

    private GeneratePayrollRunRequest BuildRequest() => new()
    {
        GovernmentEntityId = GOVERNMENT_ENTITY_ID,
        Year = pastWeek.Year,
        WeekNumber = pastWeek.WeekNumber,
        OnlyActiveEmployees = true
    };

    /// <summary>
    /// Contenedor de la ejecucion capturada. Se necesita un objeto y no una
    /// variable porque el valor lo escribe el doble del repositorio despues de que
    /// esta funcion haya devuelto.
    /// </summary>
    private sealed class CapturedPayrollRun
    {
        public PayrollRun? Value { get; set; }
    }

    /// <summary>
    /// Configura el doble del repositorio para conservar la ejecucion que se le
    /// entrega, de modo que la prueba pueda inspeccionar exactamente lo que se
    /// habria persistido. El mismo objeto se devuelve al consultar el detalle, que
    /// es lo que hace el servicio al terminar de generar.
    /// </summary>
    /// <returns>Contenedor que tendra la ejecucion tras invocar la generacion.</returns>
    private CapturedPayrollRun CaptureGeneratedRun()
    {
        CapturedPayrollRun captured = new();

        payrollRunRepository
            .When(repository => repository.AddAsync(
                Arg.Any<PayrollRun>(),
                Arg.Any<CancellationToken>()))
            .Do(callInfo => captured.Value = callInfo.Arg<PayrollRun>());

        payrollRunRepository
            .GetWithDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => captured.Value);

        return captured;
    }

    private static IReadOnlyCollection<Employee> BuildSampleEmployees()
    {
        Department technologyDepartment = new() { Name = "Tecnologia", Code = "TIC" };
        Department financeDepartment = new() { Name = "Finanzas", Code = "FIN" };

        return new List<Employee>
        {
            new SalariedEmployee
            {
                FirstName = "Ana",
                PaternalLastName = "Martinez",
                SocialSecurityNumber = "001-0000001-1",
                GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                Department = technologyDepartment,
                Status = EmployeeStatus.Active,
                WeeklySalary = 35_000m
            },
            new HourlyEmployee
            {
                PaternalLastName = "Rodriguez",
                SocialSecurityNumber = "001-0000002-2",
                GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                Department = technologyDepartment,
                Status = EmployeeStatus.Active,
                HourlyWage = 450m,
                HoursWorked = 46m
            },
            new CommissionEmployee
            {
                FirstName = "Luis",
                PaternalLastName = "Perez",
                SocialSecurityNumber = "001-0000003-3",
                GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                Department = financeDepartment,
                Status = EmployeeStatus.Active,
                GrossSales = 250_000m,
                CommissionRate = 0.08m
            },
            new BaseSalariedCommissionEmployee
            {
                FirstName = "Carmen",
                PaternalLastName = "Guzman",
                SocialSecurityNumber = "001-0000004-4",
                GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                Department = financeDepartment,
                Status = EmployeeStatus.Active,
                GrossSales = 180_000m,
                CommissionRate = 0.05m,
                BaseSalary = 20_000m
            }
        };
    }

    private static IReadOnlyCollection<Employee> BuildManyEmployees(int employeeCount)
    {
        Department department = new() { Name = "Operaciones", Code = "OPE" };
        List<Employee> employees = new(employeeCount);

        for (int index = 0; index < employeeCount; index++)
        {
            employees.Add((index % 4) switch
            {
                0 => new SalariedEmployee
                {
                    FirstName = $"Nombre{index}",
                    PaternalLastName = $"Apellido{index}",
                    SocialSecurityNumber = $"001-{index:D7}-1",
                    GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                    Department = department,
                    WeeklySalary = 20_000m + index
                },
                1 => new HourlyEmployee
                {
                    PaternalLastName = $"Apellido{index}",
                    SocialSecurityNumber = $"001-{index:D7}-2",
                    GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                    Department = department,
                    HourlyWage = 200m,
                    HoursWorked = 38m + (index % 10)
                },
                2 => new CommissionEmployee
                {
                    FirstName = $"Nombre{index}",
                    PaternalLastName = $"Apellido{index}",
                    SocialSecurityNumber = $"001-{index:D7}-3",
                    GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                    Department = department,
                    GrossSales = 100_000m + index,
                    CommissionRate = 0.05m
                },
                _ => new BaseSalariedCommissionEmployee
                {
                    FirstName = $"Nombre{index}",
                    PaternalLastName = $"Apellido{index}",
                    SocialSecurityNumber = $"001-{index:D7}-4",
                    GovernmentEntityId = GOVERNMENT_ENTITY_ID,
                    Department = department,
                    GrossSales = 90_000m + index,
                    CommissionRate = 0.04m,
                    BaseSalary = 15_000m
                }
            });
        }

        return employees;
    }
}
