using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Calculo de pagos semanales por compania.
/// </summary>
/// <remarks>
/// El flujo previsto es: consultar la vista previa de la semana, generar la
/// ejecucion y consultarla despues en el historial. Una semana solo puede
/// generarse una vez por compania.
/// </remarks>
[ApiController]
[Route("api/nomina")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MAINTENANCE_READ)]
public sealed class NominaController : ControllerBase
{
    private readonly IPayrollRunService payrollRunService;

    public NominaController(IPayrollRunService payrollRunService)
    {
        this.payrollRunService = payrollRunService;
    }

    /// <summary>
    /// Calcula la nomina de una semana sin almacenarla, para revisarla antes de
    /// generarla. Indica tambien si la semana ya fue pagada.
    /// </summary>
    /// <param name="companyId">Compania a calcular.</param>
    /// <param name="year">Ano ISO 8601 de la semana.</param>
    /// <param name="weekNumber">Numero de semana ISO 8601.</param>
    /// <param name="onlyActiveEmployees">Indica si se limita a empleados activos.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Calculo propuesto para la semana.</returns>
    /// <response code="200">Vista previa calculada correctamente.</response>
    /// <response code="400">La semana indicada no existe en el calendario.</response>
    /// <response code="404">La compania no existe.</response>
    [HttpGet("vista-previa")]
    [ProducesResponseType(typeof(PayrollPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollPreviewResponse>> ObtenerVistaPrevia(
        [FromQuery] Guid companyId,
        [FromQuery] int year,
        [FromQuery] int weekNumber,
        [FromQuery] bool onlyActiveEmployees = true,
        CancellationToken cancellationToken = default)
    {
        PayrollPreviewResponse response = await payrollRunService.PreviewAsync(
            companyId,
            year,
            weekNumber,
            onlyActiveEmployees,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Genera y almacena la nomina de una semana. Si la compania ya tiene esa
    /// semana pagada, la operacion se rechaza.
    /// </summary>
    /// <param name="request">Compania, semana y alcance del calculo.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La ejecucion generada con su detalle.</returns>
    /// <response code="201">Nomina generada correctamente.</response>
    /// <response code="400">Datos invalidos, semana futura o compania sin empleados.</response>
    /// <response code="404">La compania no existe.</response>
    /// <response code="409">La semana ya tiene nomina generada.</response>
    [HttpPost("ejecuciones")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(PayrollRunDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollRunDetailResponse>> GenerarNomina(
        [FromBody] GeneratePayrollRunRequest request,
        CancellationToken cancellationToken)
    {
        PayrollRunDetailResponse response = await payrollRunService.GenerateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerEjecucionPorIdentificador),
            new { id = response.Summary.Id },
            response);
    }

    /// <summary>
    /// Consulta el historial de nominas generadas, de la mas reciente a la mas
    /// antigua.
    /// </summary>
    /// <param name="filter">Filtros por compania, ano y paginacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina del historial.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    [HttpGet("ejecuciones")]
    [ProducesResponseType(
        typeof(PagedResponse<PayrollRunSummaryResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PayrollRunSummaryResponse>>> ConsultarHistorial(
        [FromQuery] PayrollRunFilterRequest filter,
        CancellationToken cancellationToken)
    {
        PagedResponse<PayrollRunSummaryResponse> response = await payrollRunService.SearchAsync(
            filter,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene una nomina generada con el detalle del pago de cada empleado y el
    /// desglose del calculo tal como quedo el dia en que se genero.
    /// </summary>
    /// <param name="id">Identificador de la ejecucion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La ejecucion solicitada.</returns>
    /// <response code="200">Ejecucion encontrada.</response>
    /// <response code="404">La ejecucion no existe.</response>
    [HttpGet("ejecuciones/{id:guid}")]
    [ProducesResponseType(typeof(PayrollRunDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollRunDetailResponse>> ObtenerEjecucionPorIdentificador(
        Guid id,
        CancellationToken cancellationToken)
    {
        PayrollRunDetailResponse response = await payrollRunService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Anula una nomina generada. El documento se conserva como evidencia y la
    /// semana queda libre para volver a calcularse.
    /// </summary>
    /// <param name="id">Identificador de la ejecucion.</param>
    /// <param name="request">Motivo de la anulacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La ejecucion anulada.</returns>
    /// <response code="200">Ejecucion anulada correctamente.</response>
    /// <response code="400">Falta el motivo o la ejecucion ya estaba anulada.</response>
    /// <response code="404">La ejecucion no existe.</response>
    [HttpPost("ejecuciones/{id:guid}/anular")]
    [Authorize(Policy = AuthorizationPolicies.ADMINISTRATION_ONLY)]
    [ProducesResponseType(typeof(PayrollRunDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollRunDetailResponse>> AnularEjecucion(
        Guid id,
        [FromBody] CancelPayrollRunRequest request,
        CancellationToken cancellationToken)
    {
        PayrollRunDetailResponse response = await payrollRunService.CancelAsync(
            id,
            request,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene las semanas de un ano que ya tienen nomina generada para una
    /// compania. Permite que la interfaz muestre el estado de cada semana sin
    /// recorrer el historial completo.
    /// </summary>
    /// <param name="companyId">Compania consultada.</param>
    /// <param name="year">Ano consultado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Semanas con nomina vigente.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    /// <response code="404">La compania no existe.</response>
    [HttpGet("semanas-generadas")]
    [ProducesResponseType(typeof(GeneratedWeeksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneratedWeeksResponse>> ObtenerSemanasGeneradas(
        [FromQuery] Guid companyId,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        GeneratedWeeksResponse response = await payrollRunService.GetGeneratedWeeksAsync(
            companyId,
            year,
            cancellationToken);

        return Ok(response);
    }
}
