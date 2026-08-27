using Microsoft.EntityFrameworkCore;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Infrastructure.Persistence.Configurations;

namespace SB.API_SB.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio de empleados sobre Entity Framework Core.</summary>
public sealed class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(ApplicationDbContext databaseContext)
        : base(databaseContext)
    {
    }

    /// <summary>
    /// Aplica los filtros y la paginacion como parte de la consulta SQL. El
    /// filtrado y el conteo se resuelven en el motor de base de datos: nunca se
    /// materializa la tabla completa en memoria.
    /// </summary>
    /// <inheritdoc />
    public async Task<PagedList<Employee>> SearchAsync(
        EmployeeFilterCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        (int pageNumber, int pageSize) = PagedList<Employee>.NormalizePagination(
            criteria.PageNumber,
            criteria.PageSize);

        IQueryable<Employee> query = BuildFilteredQuery(criteria);

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedList<Employee>.Empty(pageNumber, pageSize);
        }

        List<Employee> employees = await query
            .OrderBy(employee => employee.PaternalLastName)
            .ThenBy(employee => employee.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Employee>(employees, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<Employee?> GetWithDepartmentAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .Include(employee => employee.Department)
            .Include(employee => employee.Company)
            .FirstOrDefaultAsync(employee => employee.Id == employeeId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Employee>> GetForPayrollAsync(
        Guid companyId,
        bool onlyActiveEmployees,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Employee> query = EntitySet
            .AsNoTracking()
            .Include(employee => employee.Department)
            .Where(employee => employee.CompanyId == companyId);

        if (onlyActiveEmployees)
        {
            query = query.Where(employee => employee.Status == Domain.Enums.EmployeeStatus.Active);
        }

        return await query
            .OrderBy(employee => employee.PaternalLastName)
            .ThenBy(employee => employee.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsBySocialSecurityNumberAsync(
        string socialSecurityNumber,
        Guid? excludedEmployeeId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Employee> query = EntitySet
            .AsNoTracking()
            .Where(employee => employee.SocialSecurityNumber == socialSecurityNumber);

        if (excludedEmployeeId.HasValue)
        {
            query = query.Where(employee => employee.Id != excludedEmployeeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    private IQueryable<Employee> BuildFilteredQuery(EmployeeFilterCriteria criteria)
    {
        IQueryable<Employee> query = EntitySet
            .AsNoTracking()
            .Include(employee => employee.Department)
            .Include(employee => employee.Company);

        if (!string.IsNullOrWhiteSpace(criteria.Name))
        {
            string searchTerm = criteria.Name.Trim();

            query = query.Where(employee =>
                EF.Functions.Like(employee.PaternalLastName, $"%{searchTerm}%") ||
                (employee.FirstName != null &&
                 EF.Functions.Like(employee.FirstName, $"%{searchTerm}%")));
        }

        if (criteria.CompanyId.HasValue)
        {
            query = query.Where(employee => employee.CompanyId == criteria.CompanyId.Value);
        }

        if (criteria.DepartmentId.HasValue)
        {
            query = query.Where(employee => employee.DepartmentId == criteria.DepartmentId.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(employee => employee.Status == criteria.Status.Value);
        }

        if (criteria.Type.HasValue)
        {
            // El tipo se persiste en la columna discriminadora de la jerarquia, por
            // lo que se consulta como propiedad sombra y el filtro viaja al motor.
            int discriminatorValue = (int)criteria.Type.Value;

            query = query.Where(employee =>
                EF.Property<int>(employee, EmployeeConfiguration.DISCRIMINATOR_COLUMN_NAME) ==
                discriminatorValue);
        }

        return query;
    }
}
