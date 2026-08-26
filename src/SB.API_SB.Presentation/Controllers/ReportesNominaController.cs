using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Reportes de nomina. El reporte semanal detalla el calculo aplicado a cada
/// empleado segun su tipo de contrato.
/// </summary>
[ApiController]
[Route("api/reportes-nomina")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MAINTENANCE_READ)]
public sealed class ReportesNominaController : ControllerBase
{
    private readonly IPayrollReportService payrollReportService;

    public ReportesNominaController(IPayrollReportService payrollReportService)
    {
        this.payrollReportService = payrollReportService;
    }

    /// <summary>
    /// Genera el reporte semanal de pagos con el detalle del calculo por empleado y
    /// los totales por tipo de contrato y por departamento.
    /// </summary>
    /// <param name="soloEmpleadosActivos">
    /// Indica si el reporte se limita a empleados activos. Por omision es verdadero.
    /// </param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Reporte semanal de nomina.</returns>
    /// <response code="200">Reporte generado correctamente.</response>
    [HttpGet("semanal")]
    [ProducesResponseType(typeof(WeeklyPayrollReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WeeklyPayrollReportResponse>> ObtenerReporteSemanal(
        [FromQuery(Name = "soloEmpleadosActivos")] bool soloEmpleadosActivos = true,
        CancellationToken cancellationToken = default)
    {
        WeeklyPayrollReportResponse response =
            await payrollReportService.GenerateWeeklyReportAsync(
                soloEmpleadosActivos,
                cancellationToken);

        return Ok(response);
    }
}
