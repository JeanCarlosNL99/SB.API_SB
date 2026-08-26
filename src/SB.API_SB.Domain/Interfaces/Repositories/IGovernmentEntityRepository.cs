using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Criteria;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato del mantenimiento de entidades gubernamentales. El dominio declara
/// que necesita persistir entidades gubernamentales; que el almacen concreto sea
/// un archivo de texto plano es una decision de infraestructura.
/// </summary>
public interface IGovernmentEntityRepository : IRepository<GovernmentEntity>
{
    /// <summary>Busca entidades gubernamentales aplicando filtros y paginacion.</summary>
    /// <param name="criteria">Criterios de busqueda.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de entidades que satisfacen el filtro.</returns>
    Task<PagedList<GovernmentEntity>> SearchAsync(
        GovernmentEntityFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>Determina si ya existe una entidad con el nombre indicado.</summary>
    /// <param name="name">Nombre a verificar.</param>
    /// <param name="excludedEntityId">Entidad a excluir de la verificacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Verdadero si el nombre ya esta registrado.</returns>
    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedEntityId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene los valores distintos de categoria, sector y poder del Estado.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Catalogos disponibles para alimentar los filtros de la interfaz.</returns>
    Task<GovernmentEntityCatalogs> GetCatalogsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Catalogos derivados del mantenimiento de entidades gubernamentales.</summary>
/// <param name="Categories">Categorias registradas.</param>
/// <param name="Sectors">Sectores registrados.</param>
/// <param name="StateBranches">Poderes del Estado registrados.</param>
public sealed record GovernmentEntityCatalogs(
    IReadOnlyCollection<string> Categories,
    IReadOnlyCollection<string> Sectors,
    IReadOnlyCollection<string> StateBranches);
