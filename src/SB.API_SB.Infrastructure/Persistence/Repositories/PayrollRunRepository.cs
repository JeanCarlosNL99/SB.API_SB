using Microsoft.EntityFrameworkCore;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio del historial de nomina sobre Entity Framework Core.</summary>
public sealed class PayrollRunRepository : Repository<PayrollRun>, IPayrollRunRepository
{
    public PayrollRunRepository(ApplicationDbContext databaseContext)
        : base(databaseContext)
    {
    }

    /// <summary>
    /// Devuelve solo la cabecera de cada ejecucion. El detalle puede tener cientos
    /// de lineas por documento: traerlo en el listado del historial multiplicaria
    /// el volumen transferido sin que la pantalla lo use.
    /// </summary>
    /// <inheritdoc />
    public async Task<PagedList<PayrollRun>> SearchAsync(
        PayrollRunFilterCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        (int pageNumber, int pageSize) = PagedList<PayrollRun>.NormalizePagination(
            criteria.PageNumber,
            criteria.PageSize);

        IQueryable<PayrollRun> query = EntitySet.AsNoTracking();

        if (criteria.GovernmentEntityId.HasValue)
        {
            query = query.Where(payrollRun => payrollRun.GovernmentEntityId == criteria.GovernmentEntityId.Value);
        }

        if (criteria.Year.HasValue)
        {
            query = query.Where(payrollRun => payrollRun.Year == criteria.Year.Value);
        }

        if (!criteria.IncludeCancelled)
        {
            query = query.Where(payrollRun => payrollRun.Status == PayrollRunStatus.Generated);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedList<PayrollRun>.Empty(pageNumber, pageSize);
        }

        List<PayrollRun> payrollRuns = await query
            .OrderByDescending(payrollRun => payrollRun.Year)
            .ThenByDescending(payrollRun => payrollRun.WeekNumber)
            .ThenBy(payrollRun => payrollRun.GovernmentEntityName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<PayrollRun>(payrollRuns, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<PayrollRun?> GetWithDetailAsync(
        Guid payrollRunId,
        CancellationToken cancellationToken = default) =>
        EntitySet
            .Include(payrollRun => payrollRun.Lines)
                .ThenInclude(line => line.Components)
            // Sin division, una sola consulta multiplicaria las filas por el producto
            // de lineas y componentes. Con AsSplitQuery cada coleccion viaja en su
            // propia consulta y el volumen transferido crece de forma lineal.
            .AsSplitQuery()
            .FirstOrDefaultAsync(payrollRun => payrollRun.Id == payrollRunId, cancellationToken);

    /// <inheritdoc />
    public Task<PayrollRun?> FindGeneratedRunAsync(
        Guid governmentEntityId,
        PayrollWeek payrollWeek,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payrollWeek);

        return EntitySet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                payrollRun =>
                    payrollRun.GovernmentEntityId == governmentEntityId &&
                    payrollRun.Year == payrollWeek.Year &&
                    payrollRun.WeekNumber == payrollWeek.WeekNumber &&
                    payrollRun.Status == PayrollRunStatus.Generated,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<int>> GetGeneratedWeekNumbersAsync(
        Guid governmentEntityId,
        int year,
        CancellationToken cancellationToken = default) =>
        await EntitySet
            .AsNoTracking()
            .Where(payrollRun =>
                payrollRun.GovernmentEntityId == governmentEntityId &&
                payrollRun.Year == year &&
                payrollRun.Status == PayrollRunStatus.Generated)
            .Select(payrollRun => payrollRun.WeekNumber)
            .OrderBy(weekNumber => weekNumber)
            .ToListAsync(cancellationToken);
}
