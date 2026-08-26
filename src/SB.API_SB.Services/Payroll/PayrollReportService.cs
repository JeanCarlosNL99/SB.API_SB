using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Application.Mappings;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Services.Payroll;

/// <summary>
/// Genera el reporte semanal de nomina.
/// </summary>
/// <remarks>
/// El servicio no calcula pagos: pide a cada empleado su propio pago y su propio
/// desglose. Gracias al polimorfismo, un solo recorrido cubre los cuatro tipos de
/// contrato y agregar un quinto no obliga a tocar esta clase. Los empleados se
/// leen en una unica consulta sin seguimiento de cambios, lo que permite procesar
/// miles de registros en milisegundos.
/// </remarks>
public sealed class PayrollReportService : IPayrollReportService
{
    private const string UNASSIGNED_DEPARTMENT_NAME = "Sin departamento";

    private readonly IEmployeeRepository employeeRepository;
    private readonly IEmployeeTypeHandlerResolver typeHandlerResolver;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ILogger<PayrollReportService> logger;

    public PayrollReportService(
        IEmployeeRepository employeeRepository,
        IEmployeeTypeHandlerResolver typeHandlerResolver,
        IDateTimeProvider dateTimeProvider,
        ILogger<PayrollReportService> logger)
    {
        this.employeeRepository = employeeRepository;
        this.typeHandlerResolver = typeHandlerResolver;
        this.dateTimeProvider = dateTimeProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<WeeklyPayrollReportResponse> GenerateWeeklyReportAsync(
        bool onlyActiveEmployees,
        CancellationToken cancellationToken = default)
    {
        Stopwatch elapsedTimeWatch = Stopwatch.StartNew();

        IReadOnlyCollection<Employee> employees = await employeeRepository.GetForPayrollAsync(
            onlyActiveEmployees,
            cancellationToken);

        List<PayrollReportLineResponse> lines = employees
            .Select(BuildReportLine)
            .ToList();

        WeeklyPayrollReportResponse report = new()
        {
            GeneratedAtUtc = dateTimeProvider.UtcNow,
            OnlyActiveEmployees = onlyActiveEmployees,
            EmployeeCount = lines.Count,
            TotalWeeklyPayment = lines.Sum(line => line.WeeklyPayment),
            Lines = lines,
            TotalsByType = SummarizeBy(lines, line => line.TypeDescription),
            TotalsByDepartment = SummarizeBy(lines, line => line.DepartmentName)
        };

        elapsedTimeWatch.Stop();

        logger.LogInformation(
            "Reporte semanal de nomina generado. Empleados: {EmployeeCount}. " +
            "Total: {TotalWeeklyPayment}. Tiempo: {ElapsedMilliseconds} ms.",
            report.EmployeeCount,
            report.TotalWeeklyPayment,
            elapsedTimeWatch.ElapsedMilliseconds);

        return report;
    }

    private static IReadOnlyCollection<PayrollSummaryItemResponse> SummarizeBy(
        IEnumerable<PayrollReportLineResponse> lines,
        Func<PayrollReportLineResponse, string> groupSelector) =>
        lines
            .GroupBy(groupSelector)
            .Select(group => new PayrollSummaryItemResponse
            {
                GroupName = group.Key,
                EmployeeCount = group.Count(),
                TotalWeeklyPayment = group.Sum(line => line.WeeklyPayment)
            })
            .OrderByDescending(summary => summary.TotalWeeklyPayment)
            .ToList();

    private PayrollReportLineResponse BuildReportLine(Employee employee)
    {
        IEmployeeTypeHandler typeHandler = typeHandlerResolver.Resolve(employee.Type);

        return new PayrollReportLineResponse
        {
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            SocialSecurityNumber = employee.SocialSecurityNumber,
            Type = employee.Type,
            TypeDescription = typeHandler.TypeDescription,
            DepartmentName = string.IsNullOrWhiteSpace(employee.Department?.Name)
                ? UNASSIGNED_DEPARTMENT_NAME
                : employee.Department!.Name,
            Status = employee.Status,
            WeeklyPayment = employee.CalculateWeeklyPayment(),
            PaymentBreakdown = employee.BuildPaymentBreakdown().ToResponse()
        };
    }
}
