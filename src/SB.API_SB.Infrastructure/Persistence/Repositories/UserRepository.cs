using Microsoft.EntityFrameworkCore;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de usuarios sobre Entity Framework Core.</summary>
public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext databaseContext)
        : base(databaseContext)
    {
    }

    /// <inheritdoc />
    public Task<User?> GetByUserNameWithRolesAsync(
        string userName,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.UserName == userName, cancellationToken);

    /// <inheritdoc />
    public Task<User?> GetByIdWithRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<User>> GetAllWithRolesAsync(
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .AsNoTracking()
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsByUserNameOrEmailAsync(
        string userName,
        string email,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = EntitySet
            .AsNoTracking()
            .Where(user => user.UserName == userName || user.Email == email);

        if (excludedUserId.HasValue)
        {
            query = query.Where(user => user.Id != excludedUserId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }
}
