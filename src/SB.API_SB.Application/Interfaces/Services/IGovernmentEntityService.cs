using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.GovernmentEntities;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>
/// Casos de uso de la consulta del listado de entidades gubernamentales de la
/// Republica Dominicana.
/// </summary>
/// <remarks>
/// El listado es un catalogo de solo lectura: se distribuye con la aplicacion en
/// el archivo de texto plano y es la fuente a la que se asocia cada empleado.
/// </remarks>
public interface IGovernmentEntityService
{
    /// <summary>Consulta entidades gubernamentales con filtros y paginacion.</summary>
    /// <param name="filter">Filtros solicitados.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de entidades gubernamentales.</returns>
    Task<PagedResponse<GovernmentEntityResponse>> SearchAsync(
        GovernmentEntityFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene una entidad gubernamental por su identificador.</summary>
    /// <param name="entityId">Identificador del registro.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad solicitada.</returns>
    Task<GovernmentEntityResponse> GetByIdAsync(
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el listado completo de entidades activas, reducido a identificador
    /// y nombre, para alimentar los selectores de la interfaz.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Entidades activas ordenadas por nombre.</returns>
    Task<IReadOnlyCollection<GovernmentEntityOptionResponse>> GetSelectionOptionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene los catalogos que alimentan los filtros de la interfaz.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Catalogos de categoria, sector y poder del Estado.</returns>
    Task<GovernmentEntityCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);
}
