using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.GovernmentEntities;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Controllers;

/// <summary>
/// Consulta del listado de entidades gubernamentales de la Republica Dominicana,
/// persistido en un archivo de texto plano dentro del proyecto.
/// </summary>
/// <remarks>
/// El listado es un catalogo de solo lectura: se distribuye con la aplicacion y
/// es la fuente a la que se asocia cada empleado, por lo que el controlador
/// expone unicamente operaciones de consulta.
/// </remarks>
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

    /// <summary>
    /// Obtiene el listado completo de entidades activas, reducido a identificador y
    /// nombre, para alimentar los selectores de la interfaz.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Entidades activas ordenadas por nombre.</returns>
    /// <response code="200">Listado obtenido correctamente.</response>
    [HttpGet("opciones")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<GovernmentEntityOptionResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<GovernmentEntityOptionResponse>>>
        ObtenerOpciones(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<GovernmentEntityOptionResponse> response =
            await entityService.GetSelectionOptionsAsync(cancellationToken);

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
}
