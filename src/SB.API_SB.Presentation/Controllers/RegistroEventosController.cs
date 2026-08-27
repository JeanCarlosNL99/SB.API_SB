using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Contracts.EventLog;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Consulta del registro de eventos que escribe Serilog.
/// </summary>
/// <remarks>
/// Todo el controlador exige rol administrador. El registro contiene rutas
/// internas, trazas de excepciones y nombres de usuario: es informacion de
/// diagnostico, no de negocio, y no debe quedar al alcance de cualquier usuario
/// autenticado.
/// </remarks>
[ApiController]
[Route("api/registro-eventos")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.ADMINISTRATION_ONLY)]
public sealed class RegistroEventosController : ControllerBase
{
    private readonly IEventLogService eventLogService;

    public RegistroEventosController(IEventLogService eventLogService)
    {
        this.eventLogService = eventLogService;
    }

    /// <summary>Obtiene los archivos de registro disponibles.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Archivos ordenados del mas reciente al mas antiguo.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    /// <response code="403">El usuario no tiene rol administrador.</response>
    [HttpGet("archivos")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<EventLogFileResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<EventLogFileResponse>>> ConsultarArchivos(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<EventLogFileResponse> response = await eventLogService.GetFilesAsync(
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Lee las entradas del registro de eventos, de la mas reciente a la mas
    /// antigua, aplicando los filtros indicados.
    /// </summary>
    /// <param name="filter">Archivo, nivel minimo, texto y cantidad maxima.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Entradas encontradas y su conteo por nivel.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    /// <response code="403">El usuario no tiene rol administrador.</response>
    [HttpGet]
    [ProducesResponseType(typeof(EventLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventLogResponse>> Consultar(
        [FromQuery] EventLogFilterRequest filter,
        CancellationToken cancellationToken)
    {
        EventLogResponse response = await eventLogService.ReadAsync(filter, cancellationToken);

        return Ok(response);
    }
}
