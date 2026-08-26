using Microsoft.EntityFrameworkCore;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de roles sobre Entity Framework Core.</summary>
public sealed class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext databaseContext)
        : base(databaseContext)
    {
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Role>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Role>> GetByIdentifiersAsync(
        IReadOnlyCollection<Guid> roleIdentifiers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIdentifiers);

        if (roleIdentifiers.Count == 0)
        {
            return Array.Empty<Role>();
        }

        return await EntitySet
            .Where(role => roleIdentifiers.Contains(role.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        EntitySet.FirstOrDefaultAsync(role => role.Name == name, cancellationToken);
}
