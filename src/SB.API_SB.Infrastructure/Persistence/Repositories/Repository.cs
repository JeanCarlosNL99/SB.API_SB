using Microsoft.EntityFrameworkCore;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementacion generica de <see cref="IRepository{TEntity}"/> sobre Entity
/// Framework Core. Concentra las operaciones comunes para que los repositorios
/// especificos solo aporten sus consultas propias.
/// </summary>
/// <typeparam name="TEntity">Entidad administrada.</typeparam>
public abstract class Repository<TEntity> : IRepository<TEntity>
    where TEntity : AuditableEntity
{
    protected Repository(ApplicationDbContext databaseContext)
    {
        DatabaseContext = databaseContext;
    }

    /// <summary>Contexto de Entity Framework Core utilizado por el repositorio.</summary>
    protected ApplicationDbContext DatabaseContext { get; }

    /// <summary>Conjunto de entidades administrado.</summary>
    protected DbSet<TEntity> EntitySet => DatabaseContext.Set<TEntity>();

    /// <inheritdoc />
    public virtual Task<TEntity?> GetByIdAsync(
        Guid entityId,
        CancellationToken cancellationToken = default) =>
        EntitySet.FirstOrDefaultAsync(entity => entity.Id == entityId, cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyCollection<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await EntitySet.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await EntitySet.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        EntitySet.Update(entity);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        EntitySet.Remove(entity);

        return Task.CompletedTask;
    }
}
