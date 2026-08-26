using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Services;
using SB.API_SB.Tests.TestDoubles;
using Xunit;

namespace SB.API_SB.Tests.Services;

/// <summary>
/// Pruebas de las reglas de negocio del mantenimiento de empleados.
/// </summary>
/// <remarks>
/// Los repositorios se sustituyen por dobles de prueba: lo que se verifica aqui
/// son las decisiones del servicio (rechazar duplicados, exigir departamento
/// activo, impedir el cambio de tipo), no el acceso a datos. Que esto sea posible
/// sin base de datos es consecuencia directa de que el servicio dependa de
/// interfaces y no de Entity Framework.
/// </remarks>
public sealed class EmployeeServiceTests
{
    private static readonly Guid ACTIVE_DEPARTMENT_ID =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid INACTIVE_DEPARTMENT_ID =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly IEmployeeRepository employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IDepartmentRepository departmentRepository =
        Substitute.For<IDepartmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EmployeeService employeeService;

    public EmployeeServiceTests()
    {
        departmentRepository
            .GetByIdAsync(ACTIVE_DEPARTMENT_ID, Arg.Any<CancellationToken>())
            .Returns(new Department
            {
                Id = ACTIVE_DEPARTMENT_ID,
                Name = "Tecnologia de la Informacion",
                Code = "TIC",
                IsActive = true
            });

        departmentRepository
            .GetByIdAsync(INACTIVE_DEPARTMENT_ID, Arg.Any<CancellationToken>())
            .Returns(new Department
            {
                Id = INACTIVE_DEPARTMENT_ID,
                Name = "Departamento Cerrado",
                Code = "OLD",
                IsActive = false
            });

        employeeService = new EmployeeService(
            employeeRepository,
            departmentRepository,
            EmployeeTypeHandlerResolverFactory.Create(),
            unitOfWork,
            NullLogger<EmployeeService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_DatosValidos_PersisteElEmpleadoYConfirmaLaTransaccion()
    {
        Employee? persistedEmployee = null;

        await employeeRepository
            .AddAsync(Arg.Do<Employee>(employee => persistedEmployee = employee),
                Arg.Any<CancellationToken>());

        employeeRepository
            .GetWithDepartmentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => persistedEmployee);

        CreateEmployeeRequest request = BuildHourlyRequest();

        EmployeeResponse response = await employeeService.CreateAsync(request);

        Assert.NotNull(persistedEmployee);
        Assert.IsType<HourlyEmployee>(persistedEmployee);
        Assert.Equal("Diaz", persistedEmployee!.PaternalLastName);
        Assert.Equal(EmployeeType.Hourly, response.Type);

        // 40 horas ordinarias a 300 mas 5 horas extras a 450.
        Assert.Equal(14_250m, response.WeeklyPayment);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NumeroDeSeguroSocialRepetido_LanzaExcepcionDeDuplicado()
    {
        employeeRepository
            .ExistsBySocialSecurityNumberAsync(
                Arg.Any<string>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        CreateEmployeeRequest request = BuildHourlyRequest();

        await Assert.ThrowsAsync<DuplicatedEntityException>(
            () => employeeService.CreateAsync(request));

        await employeeRepository.DidNotReceive().AddAsync(
            Arg.Any<Employee>(),
            Arg.Any<CancellationToken>());

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DepartamentoInexistente_LanzaExcepcionDeNoEncontrado()
    {
        CreateEmployeeRequest request = BuildHourlyRequest();
        request.DepartmentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => employeeService.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_DepartamentoInactivo_LanzaExcepcionDeReglaDeNegocio()
    {
        CreateEmployeeRequest request = BuildHourlyRequest();
        request.DepartmentId = INACTIVE_DEPARTMENT_ID;

        BusinessRuleViolationException exception =
            await Assert.ThrowsAsync<BusinessRuleViolationException>(
                () => employeeService.CreateAsync(request));

        Assert.Contains("inactivo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_IntentoDeCambiarElTipoDeContrato_LanzaExcepcionDeReglaDeNegocio()
    {
        Guid employeeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        employeeRepository
            .GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new HourlyEmployee
            {
                Id = employeeId,
                PaternalLastName = "Diaz",
                SocialSecurityNumber = "001-9999999-9",
                DepartmentId = ACTIVE_DEPARTMENT_ID,
                HourlyWage = 300m,
                HoursWorked = 45m
            });

        UpdateEmployeeRequest request = new()
        {
            Type = EmployeeType.Salaried,
            FirstName = "Juan",
            PaternalLastName = "Diaz",
            SocialSecurityNumber = "001-9999999-9",
            DepartmentId = ACTIVE_DEPARTMENT_ID,
            Status = EmployeeStatus.Active,
            WeeklySalary = 40_000m
        };

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => employeeService.UpdateAsync(employeeId, request));
    }

    [Fact]
    public async Task UpdateAsync_NuevasHorasTrabajadas_RecalculaElPagoSemanal()
    {
        Guid employeeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        HourlyEmployee existingEmployee = new()
        {
            Id = employeeId,
            PaternalLastName = "Diaz",
            SocialSecurityNumber = "001-9999999-9",
            DepartmentId = ACTIVE_DEPARTMENT_ID,
            HourlyWage = 300m,
            HoursWorked = 40m
        };

        employeeRepository
            .GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(existingEmployee);

        employeeRepository
            .GetWithDepartmentAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(existingEmployee);

        UpdateEmployeeRequest request = new()
        {
            Type = EmployeeType.Hourly,
            PaternalLastName = "Diaz",
            SocialSecurityNumber = "001-9999999-9",
            DepartmentId = ACTIVE_DEPARTMENT_ID,
            Status = EmployeeStatus.Active,
            HourlyWage = 300m,
            HoursWorked = 50m
        };

        EmployeeResponse response = await employeeService.UpdateAsync(employeeId, request);

        // 40 horas a 300 mas 10 horas extras a 450.
        Assert.Equal(16_500m, response.WeeklyPayment);
        Assert.Equal(50m, response.HoursWorked);
    }

    [Fact]
    public async Task SearchAsync_TrasladaLosFiltrosRecibidosAlRepositorio()
    {
        employeeRepository
            .SearchAsync(
                Arg.Any<EmployeeFilterCriteria>(),
                Arg.Any<CancellationToken>())
            .Returns(PagedList<Employee>.Empty(pageNumber: 1, pageSize: 10));

        EmployeeFilterRequest filter = new()
        {
            Name = "Diaz",
            DepartmentId = ACTIVE_DEPARTMENT_ID,
            Status = EmployeeStatus.Active,
            PageNumber = 2,
            PageSize = 25
        };

        await employeeService.SearchAsync(filter);

        await employeeRepository.Received(1).SearchAsync(
            Arg.Is<EmployeeFilterCriteria>(criteria =>
                criteria.Name == "Diaz" &&
                criteria.DepartmentId == ACTIVE_DEPARTMENT_ID &&
                criteria.Status == EmployeeStatus.Active &&
                criteria.PageNumber == 2 &&
                criteria.PageSize == 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_EmpleadoInexistente_LanzaExcepcionDeNoEncontrado()
    {
        Guid employeeId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        employeeRepository
            .GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => employeeService.DeleteAsync(employeeId));
    }

    private static CreateEmployeeRequest BuildHourlyRequest() => new()
    {
        Type = EmployeeType.Hourly,
        PaternalLastName = "Diaz",
        SocialSecurityNumber = "001-9999999-9",
        DepartmentId = ACTIVE_DEPARTMENT_ID,
        Status = EmployeeStatus.Active,
        HourlyWage = 300m,
        HoursWorked = 45m
    };
}
