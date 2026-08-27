using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Contracts.EventLog;
using SB.API_SB.Application.Interfaces.EventLog;
using SB.API_SB.Application.Interfaces.Services;

namespace SB.API_SB.Services;

/// <summary>
/// Implementacion de la consulta del registro de eventos.
/// </summary>
/// <remarks>
/// El filtrado por nivel y por texto se aplica aqui, sobre las entradas ya leidas
/// del archivo, porque un archivo de texto no admite consultas. Para acotar el
/// costo, se piden al lector mas entradas de las solicitadas y se recorta despues
/// del filtro: asi un filtro por nivel Error no devuelve una pantalla vacia solo
/// porque las ultimas entradas del archivo eran informativas.
/// </remarks>
public sealed class EventLogService : IEventLogService
{
    /// <summary>
    /// Factor por el que se multiplica la cantidad solicitada al leer el archivo,
    /// para tener margen antes de aplicar los filtros.
    /// </summary>
    private const int READ_AHEAD_FACTOR = 10;

    /// <summary>Techo absoluto de entradas que se leen del archivo en una consulta.</summary>
    private const int READ_AHEAD_LIMIT = 20_000;

    private static readonly IReadOnlyDictionary<string, int> LEVEL_SEVERITIES =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Verbose"] = 0,
            ["Debug"] = 1,
            ["Information"] = 2,
            ["Warning"] = 3,
            ["Error"] = 4,
            ["Fatal"] = 5
        };

    private readonly IEventLogReader eventLogReader;
    private readonly ILogger<EventLogService> logger;

    public EventLogService(IEventLogReader eventLogReader, ILogger<EventLogService> logger)
    {
        this.eventLogReader = eventLogReader;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<EventLogFileResponse>> GetFilesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<EventLogFileDescriptor> files =
            await eventLogReader.GetFilesAsync(cancellationToken);

        return files
            .Select(file => new EventLogFileResponse
            {
                FileName = file.FileName,
                SizeInBytes = file.SizeInBytes,
                LastWriteAtUtc = file.LastWriteAtUtc
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<EventLogResponse> ReadAsync(
        EventLogFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        int requestedEntries = NormalizeRequestedEntries(filter.MaximumEntries);
        int entriesToRead = Math.Min(requestedEntries * READ_AHEAD_FACTOR, READ_AHEAD_LIMIT);

        EventLogReadResult readResult = await eventLogReader.ReadAsync(
            filter.FileName,
            entriesToRead,
            cancellationToken);

        IEnumerable<EventLogEntry> filteredEntries = ApplyFilters(readResult.Entries, filter);

        List<EventLogEntryResponse> entries = filteredEntries
            .Take(requestedEntries)
            .Select(entry => new EventLogEntryResponse
            {
                Timestamp = entry.Timestamp,
                Level = entry.Level,
                Message = entry.Message,
                CorrelationId = entry.CorrelationId,
                UserName = entry.UserName,
                SourceContext = entry.SourceContext,
                Exception = entry.Exception
            })
            .ToList();

        Dictionary<string, int> countsByLevel = entries
            .GroupBy(entry => entry.Level)
            .ToDictionary(group => group.Key, group => group.Count());

        logger.LogDebug(
            "Consulta del registro de eventos sobre {FileName}. Nivel minimo: {MinimumLevel}. " +
            "Entradas devueltas: {EntryCount}.",
            readResult.FileName,
            filter.MinimumLevel,
            entries.Count);

        return new EventLogResponse
        {
            FileName = readResult.FileName,
            EntryCount = entries.Count,
            HasMoreEntries = readResult.HasMoreEntries || entries.Count == requestedEntries,
            CountsByLevel = countsByLevel,
            Entries = entries
        };
    }

    private static int NormalizeRequestedEntries(int requestedEntries) => requestedEntries switch
    {
        <= 0 => EventLogFilterRequest.DEFAULT_MAXIMUM_ENTRIES,
        > EventLogFilterRequest.LIMIT_MAXIMUM_ENTRIES =>
            EventLogFilterRequest.LIMIT_MAXIMUM_ENTRIES,
        _ => requestedEntries
    };

    private static IEnumerable<EventLogEntry> ApplyFilters(
        IEnumerable<EventLogEntry> entries,
        EventLogFilterRequest filter)
    {
        IEnumerable<EventLogEntry> filteredEntries = entries;

        if (!string.IsNullOrWhiteSpace(filter.MinimumLevel) &&
            LEVEL_SEVERITIES.TryGetValue(filter.MinimumLevel, out int minimumSeverity))
        {
            filteredEntries = filteredEntries.Where(entry =>
                LEVEL_SEVERITIES.TryGetValue(entry.Level, out int entrySeverity) &&
                entrySeverity >= minimumSeverity);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string searchTerm = filter.SearchTerm.Trim();

            filteredEntries = filteredEntries.Where(entry =>
                entry.Message.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                (entry.Exception?.Contains(
                    searchTerm,
                    StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                (entry.CorrelationId?.Contains(
                    searchTerm,
                    StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return filteredEntries;
    }
}
