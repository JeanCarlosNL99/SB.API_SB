using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Infrastructure.Options;

namespace SB.API_SB.Infrastructure.FlatFileStorage;

/// <summary>
/// Crea la base de datos de texto plano a partir del archivo semilla cuando
/// todavia no existe.
/// </summary>
/// <remarks>
/// El archivo semilla contiene el listado oficial de entidades gubernamentales
/// extraido del documento entregado con la prueba, con solo los cuatro campos de
/// negocio. Al iniciar la aplicacion por primera vez se genera el archivo de
/// datos completo con identificadores y auditoria. De esta forma el repositorio
/// se puede clonar y ejecutar sin pasos manuales, y el archivo semilla se
/// mantiene legible y facil de comparar en el control de versiones.
/// </remarks>
public sealed class GovernmentEntityFileInitializer
{
    private const string SEED_USER_NAME = "Semilla";

    private readonly FlatFileDatabaseOptions options;
    private readonly IFlatFilePathResolver pathResolver;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ILogger<GovernmentEntityFileInitializer> logger;

    public GovernmentEntityFileInitializer(
        IOptions<FlatFileDatabaseOptions> options,
        IFlatFilePathResolver pathResolver,
        IDateTimeProvider dateTimeProvider,
        ILogger<GovernmentEntityFileInitializer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options.Value;
        this.pathResolver = pathResolver;
        this.dateTimeProvider = dateTimeProvider;
        this.logger = logger;
    }

    /// <summary>
    /// Genera el archivo de datos si no existe. Si ya existe no lo modifica, para
    /// no perder los cambios realizados desde la aplicacion.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Cantidad de entidades sembradas, o cero si no hizo falta sembrar.</returns>
    public async Task<int> InitializeAsync(CancellationToken cancellationToken = default)
    {
        string dataFilePath = pathResolver.ResolveAbsolutePath(
            options.GovernmentEntitiesFilePath);

        if (File.Exists(dataFilePath))
        {
            logger.LogInformation(
                "La base de datos de texto plano ya existe en {FilePath}. No se siembra.",
                dataFilePath);

            return 0;
        }

        string seedFilePath = pathResolver.ResolveAbsolutePath(
            options.GovernmentEntitiesSeedFilePath);

        if (!File.Exists(seedFilePath))
        {
            logger.LogError(
                "No se encontro el archivo semilla {SeedFilePath}. El mantenimiento iniciara vacio.",
                seedFilePath);

            return 0;
        }

        List<GovernmentEntity> entities = await ReadSeedEntitiesAsync(
            seedFilePath,
            cancellationToken);

        await WriteDataFileAsync(dataFilePath, entities, cancellationToken);

        logger.LogInformation(
            "Se sembraron {EntityCount} entidades gubernamentales en {FilePath}.",
            entities.Count,
            dataFilePath);

        return entities.Count;
    }

    private async Task<List<GovernmentEntity>> ReadSeedEntitiesAsync(
        string seedFilePath,
        CancellationToken cancellationToken)
    {
        string[] seedLines = await File.ReadAllLinesAsync(seedFilePath, cancellationToken);

        DateTime seedDateTime = dateTimeProvider.UtcNow;
        List<GovernmentEntity> entities = new(seedLines.Length);

        foreach (string line in seedLines)
        {
            if (FlatFileRecordSerializer.IsIgnorableLine(line))
            {
                continue;
            }

            GovernmentEntity? entity = GovernmentEntityRecordMapper.FromSeedRecord(
                line,
                seedDateTime,
                SEED_USER_NAME);

            if (entity is not null)
            {
                entities.Add(entity);
            }
        }

        return entities;
    }

    private static async Task WriteDataFileAsync(
        string dataFilePath,
        IReadOnlyCollection<GovernmentEntity> entities,
        CancellationToken cancellationToken)
    {
        string? directoryPath = Path.GetDirectoryName(dataFilePath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        List<string> lines = new(
            GovernmentEntityRecordMapper.FileHeaderLines.Count + entities.Count);

        lines.AddRange(GovernmentEntityRecordMapper.FileHeaderLines);
        lines.AddRange(entities
            .OrderBy(entity => entity.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(GovernmentEntityRecordMapper.ToRecord));

        await File.WriteAllLinesAsync(dataFilePath, lines, cancellationToken);
    }
}
