using Microsoft.EntityFrameworkCore;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de departamentos sobre Entity Framework Core.</summary>
public sealed class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(ApplicationDbContext databaseContext)
        : base(databaseContext)
    {
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Department>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .AsNoTracking()
            .Include(department => department.Employees)
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Department?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .AsNoTracking()
            .FirstOrDefaultAsync(department => department.Code == code, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasEmployeesAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        DatabaseContext.Employees
            .AsNoTracking()
            .AnyAsync(employee => employee.DepartmentId == departmentId, cancellationToken);
}
