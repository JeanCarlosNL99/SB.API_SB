using SB.API_SB.Domain.Common;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>
/// Contrato generico de acceso a datos. Vive en el dominio y se implementa en
/// infraestructura: esa inversion de dependencias es la que permite sustituir la
/// tecnologia de persistencia sin tocar la logica de negocio.
/// </summary>
/// <typeparam name="TEntity">Entidad administrada por el repositorio.</typeparam>
public interface IRepository<TEntity>
    where TEntity : AuditableEntity
{
    /// <summary>Obtiene una entidad por su identificador.</summary>
    /// <param name="entityId">Identificador de la entidad.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad encontrada o nulo si no existe.</returns>
    Task<TEntity?> GetByIdAsync(Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene todas las entidades.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Coleccion de entidades.</returns>
    Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Agrega una nueva entidad.</summary>
    /// <param name="entity">Entidad a agregar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Actualiza una entidad existente.</summary>
    /// <param name="entity">Entidad a actualizar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Elimina una entidad existente.</summary>
    /// <param name="entity">Entidad a eliminar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
