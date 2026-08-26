using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.GovernmentEntities;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Mantenimiento del listado de entidades gubernamentales de la Republica
/// Dominicana, persistido en un archivo de texto plano dentro del proyecto.
/// </summary>
[ApiController]
[Route("api/entidades-gubernamentales")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MAINTENANCE_READ)]
public sealed class EntidadesGubernamentalesController : ControllerBase
{
    private readonly IGovernmentEntityService entityService;

    public EntidadesGubernamentalesController(IGovernmentEntityService entityService)
    {
        this.entityService = entityService;
    }

    /// <summary>Consulta entidades gubernamentales con filtros y paginacion.</summary>
    /// <param name="filter">Filtros por nombre, categoria, sector, poder y estado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de entidades gubernamentales.</returns>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<GovernmentEntityResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<GovernmentEntityResponse>>> Consultar(
        [FromQuery] GovernmentEntityFilterRequest filter,
        CancellationToken cancellationToken)
    {
        PagedResponse<GovernmentEntityResponse> response = await entityService.SearchAsync(
            filter,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene los valores distintos de categoria, sector y poder del Estado para
    /// alimentar los filtros de la interfaz.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Catalogos disponibles.</returns>
    /// <response code="200">Catalogos obtenidos correctamente.</response>
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(GovernmentEntityCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GovernmentEntityCatalogsResponse>> ObtenerCatalogos(
        CancellationToken cancellationToken)
    {
        GovernmentEntityCatalogsResponse response = await entityService.GetCatalogsAsync(
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Obtiene una entidad gubernamental por su identificador.</summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad solicitada.</returns>
    /// <response code="200">Entidad encontrada.</response>
    /// <response code="404">La entidad no existe.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GovernmentEntityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GovernmentEntityResponse>> ObtenerPorIdentificador(
        Guid id,
        CancellationToken cancellationToken)
    {
        GovernmentEntityResponse response = await entityService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Registra una nueva entidad gubernamental.</summary>
    /// <param name="request">Datos de la entidad a registrar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad registrada.</returns>
    /// <response code="201">Entidad creada correctamente.</response>
    /// <response code="400">Los datos enviados no son validos.</response>
    /// <response code="409">Ya existe una entidad con el mismo nombre.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(GovernmentEntityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GovernmentEntityResponse>> Crear(
        [FromBody] CreateGovernmentEntityRequest request,
        CancellationToken cancellationToken)
    {
        GovernmentEntityResponse response = await entityService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorIdentificador),
            new { id = response.Id },
            response);
    }

    /// <summary>Actualiza una entidad gubernamental existente.</summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="request">Nuevos datos de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad actualizada.</returns>
    /// <response code="200">Entidad actualizada correctamente.</response>
    /// <response code="404">La entidad no existe.</response>
    /// <response code="409">Ya existe otra entidad con el mismo nombre.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(typeof(GovernmentEntityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GovernmentEntityResponse>> Actualizar(
        Guid id,
        [FromBody] UpdateGovernmentEntityRequest request,
        CancellationToken cancellationToken)
    {
        GovernmentEntityResponse response = await entityService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Elimina una entidad gubernamental.</summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <response code="204">Entidad eliminada correctamente.</response>
    /// <response code="404">La entidad no existe.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MAINTENANCE_WRITE)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await entityService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
