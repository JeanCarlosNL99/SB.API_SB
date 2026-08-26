namespace SB.API_SB.Application.Contracts.Payroll;

/// <summary>
/// Reporte semanal de nomina. Incluye el detalle por empleado y totales
/// agregados por tipo de contrato y por departamento.
/// </summary>
public sealed class WeeklyPayrollReportResponse
{
    /// <summary>Fecha y hora (UTC) en que se genero el reporte.</summary>
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>Indica si el reporte se limito a empleados activos.</summary>
    public bool OnlyActiveEmployees { get; set; }

    /// <summary>Cantidad de empleados incluidos en el reporte.</summary>
    public int EmployeeCount { get; set; }

    /// <summary>Monto total de la nomina semanal.</summary>
    public decimal TotalWeeklyPayment { get; set; }

    /// <summary>Detalle por empleado.</summary>
    public IReadOnlyCollection<PayrollReportLineResponse> Lines { get; set; } =
        Array.Empty<PayrollReportLineResponse>();

    /// <summary>Totales agrupados por tipo de contrato.</summary>
    public IReadOnlyCollection<PayrollSummaryItemResponse> TotalsByType { get; set; } =
        Array.Empty<PayrollSummaryItemResponse>();

    /// <summary>Totales agrupados por departamento.</summary>
    public IReadOnlyCollection<PayrollSummaryItemResponse> TotalsByDepartment { get; set; } =
        Array.Empty<PayrollSummaryItemResponse>();
}
