using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Infrastructure.Options;

namespace SB.API_SB.Infrastructure.FlatFileStorage;

/// <summary>
/// Repositorio de consulta del listado de entidades gubernamentales, respaldado
/// por un archivo de texto plano ubicado dentro del proyecto.
/// </summary>
/// <remarks>
/// El listado oficial es un catalogo: la aplicacion lo consulta y no lo
/// administra, por lo que el repositorio no expone operaciones de escritura. Un
/// archivo de texto no ofrece control de concurrencia, de modo que la carga
/// inicial se serializa con un semaforo y el resultado queda en una cache en
/// memoria; a partir de ahi las consultas no vuelven a tocar el disco. El
/// repositorio se registra como singleton para que esa cache la comparta toda la
/// aplicacion.
/// </remarks>
public sealed class GovernmentEntityFileRepository : IGovernmentEntityRepository, IDisposable
{
    private readonly SemaphoreSlim fileAccessSemaphore = new(initialCount: 1, maxCount: 1);
    private readonly FlatFileDatabaseOptions options;
    private readonly IFlatFilePathResolver pathResolver;
    private readonly ILogger<GovernmentEntityFileRepository> logger;

    private List<GovernmentEntity>? cachedEntities;

    public GovernmentEntityFileRepository(
        IOptions<FlatFileDatabaseOptions> options,
        IFlatFilePathResolver pathResolver,
        ILogger<GovernmentEntityFileRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options.Value;
        this.pathResolver = pathResolver;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<GovernmentEntity?> GetByIdAsync(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<GovernmentEntity> entities = await ReadAllAsync(cancellationToken);

        return entities.FirstOrDefault(entity => entity.Id == entityId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<GovernmentEntity>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await ReadAllAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<PagedList<GovernmentEntity>> SearchAsync(
        GovernmentEntityFilterCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        (int pageNumber, int pageSize) = PagedList<GovernmentEntity>.NormalizePagination(
            criteria.PageNumber,
            criteria.PageSize);

        IReadOnlyCollection<GovernmentEntity> entities = await ReadAllAsync(cancellationToken);

        List<GovernmentEntity> filteredEntities = ApplyFilters(entities, criteria)
            .OrderBy(entity => entity.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (filteredEntities.Count == 0)
        {
            return PagedList<GovernmentEntity>.Empty(pageNumber, pageSize);
        }

        List<GovernmentEntity> pageItems = filteredEntities
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<GovernmentEntity>(
            pageItems,
            filteredEntities.Count,
            pageNumber,
            pageSize);
    }


    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdentifierAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<GovernmentEntity> entities = await ReadAllAsync(cancellationToken);

        return entities.ToDictionary(entity => entity.Id, entity => entity.Name);
    }
    /// <inheritdoc />
    public async Task<GovernmentEntityCatalogs> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<GovernmentEntity> entities = await ReadAllAsync(cancellationToken);

        return new GovernmentEntityCatalogs(
            Categories: ExtractDistinctValues(entities, entity => entity.Category),
            Sectors: ExtractDistinctValues(entities, entity => entity.Sector),
            StateBranches: ExtractDistinctValues(entities, entity => entity.StateBranch));
    }

    /// <inheritdoc />
    public void Dispose() => fileAccessSemaphore.Dispose();

    private static IEnumerable<GovernmentEntity> ApplyFilters(
        IEnumerable<GovernmentEntity> entities,
        GovernmentEntityFilterCriteria criteria)
    {
        IEnumerable<GovernmentEntity> filteredEntities = entities;

        if (!string.IsNullOrWhiteSpace(criteria.Name))
        {
            string searchTerm = criteria.Name.Trim();

            filteredEntities = filteredEntities.Where(entity =>
                entity.Name.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            filteredEntities = filteredEntities.Where(entity => string.Equals(
                entity.Category,
                criteria.Category,
                StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Sector))
        {
            filteredEntities = filteredEntities.Where(entity => string.Equals(
                entity.Sector,
                criteria.Sector,
                StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.StateBranch))
        {
            filteredEntities = filteredEntities.Where(entity => string.Equals(
                entity.StateBranch,
                criteria.StateBranch,
                StringComparison.CurrentCultureIgnoreCase));
        }

        if (criteria.Status.HasValue)
        {
            filteredEntities = filteredEntities.Where(entity =>
                entity.Status == criteria.Status.Value);
        }

        return filteredEntities;
    }

    private static IReadOnlyCollection<string> ExtractDistinctValues(
        IEnumerable<GovernmentEntity> entities,
        Func<GovernmentEntity, string> valueSelector) =>
        entities
            .Select(valueSelector)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private async Task<IReadOnlyCollection<GovernmentEntity>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        await fileAccessSemaphore.WaitAsync(cancellationToken);

        try
        {
            cachedEntities ??= await LoadFromFileAsync(cancellationToken);

            // Se devuelve una copia para que ningun consumidor pueda alterar la
            // cache interna del repositorio.
            return cachedEntities.ToList();
        }
        finally
        {
            fileAccessSemaphore.Release();
        }
    }

    private async Task<List<GovernmentEntity>> LoadFromFileAsync(
        CancellationToken cancellationToken)
    {
        string filePath = pathResolver.ResolveAbsolutePath(options.GovernmentEntitiesFilePath);

        if (!File.Exists(filePath))
        {
            logger.LogWarning(
                "El archivo de datos {FilePath} no existe. Se devuelve un listado vacio.",
                filePath);

            return new List<GovernmentEntity>();
        }

        string[] lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

        List<GovernmentEntity> entities = new(lines.Length);
        int malformedLineCount = 0;

        foreach (string line in lines)
        {
            if (FlatFileRecordSerializer.IsIgnorableLine(line))
            {
                continue;
            }

            GovernmentEntity? entity = GovernmentEntityRecordMapper.FromRecord(line);

            if (entity is null)
            {
                malformedLineCount++;
                continue;
            }

            entities.Add(entity);
        }

        if (malformedLineCount > 0)
        {
            logger.LogWarning(
                "Se ignoraron {MalformedLineCount} lineas malformadas del archivo {FilePath}.",
                malformedLineCount,
                filePath);
        }

        logger.LogDebug(
            "Se cargaron {EntityCount} entidades gubernamentales desde {FilePath}.",
            entities.Count,
            filePath);

        return entities;
    }
}
