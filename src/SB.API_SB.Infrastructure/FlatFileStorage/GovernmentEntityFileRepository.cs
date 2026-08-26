using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Infrastructure.Options;

namespace SB.API_SB.Infrastructure.FlatFileStorage;

/// <summary>
/// Repositorio del mantenimiento de entidades gubernamentales respaldado por un
/// archivo de texto plano ubicado dentro del proyecto.
/// </summary>
/// <remarks>
/// Un archivo de texto no ofrece transacciones ni control de concurrencia, por lo
/// que esta implementacion agrega tres garantias: acceso serializado mediante un
/// semaforo, escritura atomica sobre un archivo temporal que luego reemplaza al
/// original, y una cache en memoria que evita releer el archivo en cada consulta.
/// El repositorio se registra como singleton para que esas garantias cubran a
/// todas las peticiones de la aplicacion.
/// </remarks>
public sealed class GovernmentEntityFileRepository : IGovernmentEntityRepository, IDisposable
{
    private const string TEMPORARY_FILE_EXTENSION = ".tmp";
    private const string BACKUP_FILE_EXTENSION = ".bak";
    private const string BACKUP_TIMESTAMP_FORMAT = "yyyyMMddHHmmssfff";

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
    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<GovernmentEntity> entities = await ReadAllAsync(cancellationToken);

        return entities.Any(entity =>
            string.Equals(entity.Name, name, StringComparison.CurrentCultureIgnoreCase) &&
            (!excludedEntityId.HasValue || entity.Id != excludedEntityId.Value));
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
    public async Task AddAsync(
        GovernmentEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await MutateAsync(entities => entities.Add(entity), cancellationToken);

        logger.LogInformation(
            "Entidad gubernamental {EntityId} agregada al archivo de texto plano.",
            entity.Id);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        GovernmentEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await MutateAsync(
            entities =>
            {
                int existingIndex = entities.FindIndex(item => item.Id == entity.Id);

                if (existingIndex >= 0)
                {
                    entities[existingIndex] = entity;
                }
            },
            cancellationToken);

        logger.LogInformation(
            "Entidad gubernamental {EntityId} actualizada en el archivo de texto plano.",
            entity.Id);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        GovernmentEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await MutateAsync(
            entities => entities.RemoveAll(item => item.Id == entity.Id),
            cancellationToken);

        logger.LogInformation(
            "Entidad gubernamental {EntityId} eliminada del archivo de texto plano.",
            entity.Id);
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

    private async Task MutateAsync(
        Action<List<GovernmentEntity>> mutation,
        CancellationToken cancellationToken)
    {
        await fileAccessSemaphore.WaitAsync(cancellationToken);

        try
        {
            cachedEntities ??= await LoadFromFileAsync(cancellationToken);

            mutation(cachedEntities);

            await WriteToFileAsync(cachedEntities, cancellationToken);
        }
        catch
        {
            // Si la escritura falla se descarta la cache: la siguiente lectura
            // volvera a la unica fuente de verdad, que es el archivo.
            cachedEntities = null;
            throw;
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

    private async Task WriteToFileAsync(
        IReadOnlyCollection<GovernmentEntity> entities,
        CancellationToken cancellationToken)
    {
        string filePath = pathResolver.ResolveAbsolutePath(options.GovernmentEntitiesFilePath);
        string? directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (options.CreateBackupOnWrite && File.Exists(filePath))
        {
            CreateBackup(filePath);
        }

        List<string> lines = new(
            GovernmentEntityRecordMapper.FileHeaderLines.Count + entities.Count);

        lines.AddRange(GovernmentEntityRecordMapper.FileHeaderLines);
        lines.AddRange(entities
            .OrderBy(entity => entity.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(GovernmentEntityRecordMapper.ToRecord));

        // Escritura atomica: primero se escribe un archivo temporal completo y
        // solo si tiene exito se reemplaza el archivo de datos. Asi una
        // interrupcion no deja la base de datos a medio escribir.
        string temporaryFilePath = filePath + TEMPORARY_FILE_EXTENSION;

        await File.WriteAllLinesAsync(temporaryFilePath, lines, cancellationToken);

        File.Move(temporaryFilePath, filePath, overwrite: true);

        logger.LogDebug(
            "Se escribieron {EntityCount} entidades gubernamentales en {FilePath}.",
            entities.Count,
            filePath);
    }

    private void CreateBackup(string filePath)
    {
        try
        {
            string backupDirectoryPath =
                pathResolver.ResolveAbsolutePath(options.BackupDirectoryPath);

            Directory.CreateDirectory(backupDirectoryPath);

            string backupFileName = string.Concat(
                Path.GetFileNameWithoutExtension(filePath),
                ".",
                DateTime.UtcNow.ToString(BACKUP_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture),
                BACKUP_FILE_EXTENSION);

            File.Copy(filePath, Path.Combine(backupDirectoryPath, backupFileName), overwrite: true);
        }
        catch (IOException exception)
        {
            // Una copia de respaldo fallida no debe impedir la operacion de
            // negocio, pero si debe quedar registrada en el log.
            logger.LogWarning(
                exception,
                "No se pudo crear la copia de respaldo del archivo {FilePath}.",
                filePath);
        }
    }
}
