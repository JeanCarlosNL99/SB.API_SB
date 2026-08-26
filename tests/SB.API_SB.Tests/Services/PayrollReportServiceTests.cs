using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Services.Payroll;
using SB.API_SB.Tests.TestDoubles;
using Xunit;

namespace SB.API_SB.Tests.Services;

/// <summary>
/// Pruebas del reporte semanal de nomina, incluido el requisito no funcional de
/// rendimiento.
/// </summary>
public sealed class PayrollReportServiceTests
{
    private const int PERFORMANCE_EMPLOYEE_COUNT = 1_000;
    private const int PERFORMANCE_LIMIT_IN_MILLISECONDS = 2_000;

    private readonly IEmployeeRepository employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly PayrollReportService payrollReportService;

    public PayrollReportServiceTests()
    {
        payrollReportService = new PayrollReportService(
            employeeRepository,
            EmployeeTypeHandlerResolverFactory.Create(),
            new FixedDateTimeProvider(),
            NullLogger<PayrollReportService>.Instance);
    }

    [Fact]
    public async Task GenerateWeeklyReportAsync_ConLosCuatroTipos_TotalizaCorrectamente()
    {
        employeeRepository
            .GetForPayrollAsync(true, Arg.Any<CancellationToken>())
            .Returns(BuildSampleEmployees());

        WeeklyPayrollReportResponse report =
            await payrollReportService.GenerateWeeklyReportAsync(onlyActiveEmployees: true);

        // 35,000 asalariado + 22,050 por horas + 20,000 por comision + 31,000
        // asalariado por comision.
        Assert.Equal(4, report.EmployeeCount);
        Assert.Equal(108_050m, report.TotalWeeklyPayment);
        Assert.Equal(report.TotalWeeklyPayment, report.Lines.Sum(line => line.WeeklyPayment));
        Assert.True(report.OnlyActiveEmployees);
        Assert.Equal(FixedDateTimeProvider.DEFAULT_DATE_TIME, report.GeneratedAtUtc);
    }

    [Fact]
    public async Task GenerateWeeklyReportAsync_IncluyeElDesgloseDelCalculoDeCadaEmpleado()
    {
        employeeRepository
            .GetForPayrollAsync(true, Arg.Any<CancellationToken>())
            .Returns(BuildSampleEmployees());

        WeeklyPayrollReportResponse report =
            await payrollReportService.GenerateWeeklyReportAsync(onlyActiveEmployees: true);

        PayrollReportLineResponse hourlyLine = report.Lines
            .Single(line => line.Type == EmployeeType.Hourly);

        Assert.Contains("sueldoPorHora", hourlyLine.PaymentBreakdown.Formula);
        Assert.Equal(2, hourlyLine.PaymentBreakdown.Components.Count);
        Assert.Contains(
            hourlyLine.PaymentBreakdown.Components,
            component => component.Concept == "Horas extras");
    }

    [Fact]
    public async Task GenerateWeeklyReportAsync_AgrupaPorTipoDeContratoYPorDepartamento()
    {
        employeeRepository
            .GetForPayrollAsync(true, Arg.Any<CancellationToken>())
            .Returns(BuildSampleEmployees());

        WeeklyPayrollReportResponse report =
            await payrollReportService.GenerateWeeklyReportAsync(onlyActiveEmployees: true);

        Assert.Equal(4, report.TotalsByType.Count);
        Assert.Equal(2, report.TotalsByDepartment.Count);
        Assert.Equal(
            report.TotalWeeklyPayment,
            report.TotalsByType.Sum(summary => summary.TotalWeeklyPayment));
        Assert.Equal(
            report.TotalWeeklyPayment,
            report.TotalsByDepartment.Sum(summary => summary.TotalWeeklyPayment));
    }

    [Fact]
    public async Task GenerateWeeklyReportAsync_SinEmpleados_DevuelveReporteVacioSinFallar()
    {
        employeeRepository
            .GetForPayrollAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());

        WeeklyPayrollReportResponse report =
            await payrollReportService.GenerateWeeklyReportAsync(onlyActiveEmployees: true);

        Assert.Equal(0, report.EmployeeCount);
        Assert.Equal(0m, report.TotalWeeklyPayment);
        Assert.Empty(report.Lines);
    }

    /// <summary>
    /// Verifica el requisito no funcional: procesar los calculos de hasta 1,000
    /// empleados en menos de 2 segundos.
    /// </summary>
    [Fact]
    public async Task GenerateWeeklyReportAsync_ConMilEmpleados_TerminaEnMenosDeDosSegundos()
    {
        employeeRepository
            .GetForPayrollAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(BuildManyEmployees(PERFORMANCE_EMPLOYEE_COUNT));

        Stopwatch elapsedTimeWatch = Stopwatch.StartNew();

        WeeklyPayrollReportResponse report =
            await payrollReportService.GenerateWeeklyReportAsync(onlyActiveEmployees: false);

        elapsedTimeWatch.Stop();

        Assert.Equal(PERFORMANCE_EMPLOYEE_COUNT, report.EmployeeCount);
        Assert.True(
            elapsedTimeWatch.ElapsedMilliseconds < PERFORMANCE_LIMIT_IN_MILLISECONDS,
            $"El reporte de {PERFORMANCE_EMPLOYEE_COUNT} empleados tardo " +
            $"{elapsedTimeWatch.ElapsedMilliseconds} ms, por encima del limite de " +
            $"{PERFORMANCE_LIMIT_IN_MILLISECONDS} ms.");
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
                Department = technologyDepartment,
                Status = EmployeeStatus.Active,
                WeeklySalary = 35_000m
            },
            new HourlyEmployee
            {
                PaternalLastName = "Rodriguez",
                SocialSecurityNumber = "001-0000002-2",
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
                    PaternalLastName = $"Apellido{index}",
                    FirstName = $"Nombre{index}",
                    SocialSecurityNumber = $"001-{index:D7}-1",
                    Department = department,
                    WeeklySalary = 20_000m + index
                },
                1 => new HourlyEmployee
                {
                    PaternalLastName = $"Apellido{index}",
                    SocialSecurityNumber = $"001-{index:D7}-2",
                    Department = department,
                    HourlyWage = 200m,
                    HoursWorked = 38m + (index % 10)
                },
                2 => new CommissionEmployee
                {
                    PaternalLastName = $"Apellido{index}",
                    FirstName = $"Nombre{index}",
                    SocialSecurityNumber = $"001-{index:D7}-3",
                    Department = department,
                    GrossSales = 100_000m + index,
                    CommissionRate = 0.05m
                },
                _ => new BaseSalariedCommissionEmployee
                {
                    PaternalLastName = $"Apellido{index}",
                    FirstName = $"Nombre{index}",
                    SocialSecurityNumber = $"001-{index:D7}-4",
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
