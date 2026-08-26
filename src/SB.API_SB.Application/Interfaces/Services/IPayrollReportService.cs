using SB.API_SB.Application.Contracts.Payroll;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>Caso de uso de generacion del reporte semanal de nomina.</summary>
public interface IPayrollReportService
{
    /// <summary>
    /// Genera el reporte semanal de pagos, detallando el calculo aplicado a cada
    /// empleado segun su tipo de contrato.
    /// </summary>
    /// <param name="onlyActiveEmployees">Indica si se incluyen solo empleados activos.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Reporte semanal con detalle y totales.</returns>
    Task<WeeklyPayrollReportResponse> GenerateWeeklyReportAsync(
        bool onlyActiveEmployees,
        CancellationToken cancellationToken = default);
}
