using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Criteria;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato de consulta del listado de entidades gubernamentales. El dominio
/// declara que necesita leer entidades gubernamentales; que el almacen concreto
/// sea un archivo de texto plano es una decision de infraestructura.
/// </summary>
/// <remarks>
/// El contrato es deliberadamente de solo lectura y no hereda de
/// <see cref="IRepository{TEntity}"/>. El listado oficial de entidades
/// gubernamentales es un catalogo: se distribuye con la aplicacion y la
/// aplicacion lo consulta, no lo administra. Declarar operaciones de escritura
/// que nadie implementa ni consume solo invitaria a usarlas.
/// </remarks>
public interface IGovernmentEntityRepository
{
    /// <summary>Obtiene una entidad gubernamental por su identificador.</summary>
    /// <param name="entityId">Identificador de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad encontrada o nulo si no existe.</returns>
    Task<GovernmentEntity?> GetByIdAsync(
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene el listado completo de entidades gubernamentales.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Todas las entidades del catalogo.</returns>
    Task<IReadOnlyCollection<GovernmentEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Busca entidades gubernamentales aplicando filtros y paginacion.</summary>
    /// <param name="criteria">Criterios de busqueda.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de entidades que satisfacen el filtro.</returns>
    Task<PagedList<GovernmentEntity>> SearchAsync(
        GovernmentEntityFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el nombre de cada entidad indexado por su identificador.
    /// </summary>
    /// <remarks>
    /// Los empleados y las nominas viven en la base de datos relacional y las
    /// entidades gubernamentales en el archivo de texto plano, de modo que no hay
    /// ninguna consulta que pueda unirlos. Esta proyeccion es la que permite a la
    /// capa de servicios resolver el nombre de la entidad de un listado completo
    /// de empleados con una sola lectura del catalogo, en lugar de una por
    /// empleado.
    /// </remarks>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Nombres de las entidades indexados por identificador.</returns>
    Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdentifierAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene los valores distintos de categoria, sector y poder del Estado.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Catalogos disponibles para alimentar los filtros de la interfaz.</returns>
    Task<GovernmentEntityCatalogs> GetCatalogsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Catalogos derivados del listado de entidades gubernamentales.</summary>
/// <param name="Categories">Categorias registradas.</param>
/// <param name="Sectors">Sectores registrados.</param>
/// <param name="StateBranches">Poderes del Estado registrados.</param>
public sealed record GovernmentEntityCatalogs(
    IReadOnlyCollection<string> Categories,
    IReadOnlyCollection<string> Sectors,
    IReadOnlyCollection<string> StateBranches);
