using Microsoft.EntityFrameworkCore;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de companias sobre Entity Framework Core.</summary>
public sealed class CompanyRepository : Repository<Company>, ICompanyRepository
{
    public CompanyRepository(ApplicationDbContext databaseContext)
        : base(databaseContext)
    {
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Company>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .AsNoTracking()
            .OrderBy(company => company.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Company?> GetByTaxIdentificationNumberAsync(
        string taxIdentificationNumber,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                company => company.TaxIdentificationNumber == taxIdentificationNumber,
                cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasEmployeesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        DatabaseContext.Employees
            .AsNoTracking()
            .AnyAsync(employee => employee.CompanyId == companyId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasPayrollRunsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        DatabaseContext.PayrollRuns
            .AsNoTracking()
            .AnyAsync(payrollRun => payrollRun.CompanyId == companyId, cancellationToken);

    /// <summary>
    /// Cuenta los empleados activos agrupando en la base de datos. Una sola
    /// consulta agregada evita el problema de las N+1 que produciria contar los
    /// empleados de cada compania por separado.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetActiveEmployeeCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var counts = await DatabaseContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Status == EmployeeStatus.Active)
            .GroupBy(employee => employee.CompanyId)
            .Select(group => new { CompanyId = group.Key, EmployeeCount = group.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(item => item.CompanyId, item => item.EmployeeCount);
    }
}
