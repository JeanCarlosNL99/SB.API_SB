using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.GovernmentEntities;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>
/// Casos de uso del mantenimiento de entidades gubernamentales de la Republica
/// Dominicana.
/// </summary>
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

    /// <summary>Registra una nueva entidad gubernamental.</summary>
    /// <param name="request">Datos de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad registrada.</returns>
    Task<GovernmentEntityResponse> CreateAsync(
        CreateGovernmentEntityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Actualiza una entidad gubernamental existente.</summary>
    /// <param name="entityId">Identificador del registro.</param>
    /// <param name="request">Nuevos datos de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad actualizada.</returns>
    Task<GovernmentEntityResponse> UpdateAsync(
        Guid entityId,
        UpdateGovernmentEntityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Elimina una entidad gubernamental.</summary>
    /// <param name="entityId">Identificador del registro.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task DeleteAsync(Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene los catalogos que alimentan los filtros de la interfaz.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Catalogos de categoria, sector y poder del Estado.</returns>
    Task<GovernmentEntityCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);
}
