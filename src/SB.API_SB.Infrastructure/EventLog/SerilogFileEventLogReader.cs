using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SB.API_SB.Application.Interfaces.EventLog;
using SB.API_SB.Infrastructure.Options;

namespace SB.API_SB.Infrastructure.EventLog;

/// <summary>
/// Lee el registro de eventos desde los archivos que escribe Serilog en formato
/// JSON compacto.
/// </summary>
/// <remarks>
/// Serilog escribe dos archivos por dia: uno de texto, pensado para leerse en una
/// terminal, y uno de JSON por linea, pensado para procesarse. Esta clase lee el
/// segundo: analizar JSON estructurado es fiable, mientras que aplicar expresiones
/// regulares sobre el archivo de texto se rompe con cualquier mensaje que contenga
/// un salto de linea.
///
/// Solo se leen las ultimas lineas del archivo. Un registro diario puede tener
/// decenas de miles de entradas y la pantalla muestra las mas recientes: cargar el
/// archivo completo en memoria para descartar el 99% seria un desperdicio.
/// </remarks>
public sealed class SerilogFileEventLogReader : IEventLogReader
{
    private const string LOG_FILE_SEARCH_PATTERN = "*.json";
    private const int READ_BUFFER_SIZE_IN_BYTES = 16 * 1024;

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly EventLogOptions options;
    private readonly IEventLogPathResolver pathResolver;
    private readonly ILogger<SerilogFileEventLogReader> logger;

    public SerilogFileEventLogReader(
        IOptions<EventLogOptions> options,
        IEventLogPathResolver pathResolver,
        ILogger<SerilogFileEventLogReader> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options.Value;
        this.pathResolver = pathResolver;
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<EventLogFileDescriptor>> GetFilesAsync(
        CancellationToken cancellationToken = default)
    {
        string directoryPath = pathResolver.ResolveAbsolutePath(options.DirectoryPath);

        if (!Directory.Exists(directoryPath))
        {
            logger.LogWarning(
                "El directorio de registros {DirectoryPath} no existe.",
                directoryPath);

            return Task.FromResult<IReadOnlyCollection<EventLogFileDescriptor>>(
                Array.Empty<EventLogFileDescriptor>());
        }

        List<EventLogFileDescriptor> files = Directory
            .EnumerateFiles(directoryPath, LOG_FILE_SEARCH_PATTERN)
            .Select(filePath => new FileInfo(filePath))
            .OrderByDescending(fileInfo => fileInfo.LastWriteTimeUtc)
            .Select(fileInfo => new EventLogFileDescriptor(
                fileInfo.Name,
                fileInfo.Length,
                fileInfo.LastWriteTimeUtc))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<EventLogFileDescriptor>>(files);
    }

    /// <inheritdoc />
    public async Task<EventLogReadResult> ReadAsync(
        string? fileName,
        int maximumEntries,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<EventLogFileDescriptor> availableFiles =
            await GetFilesAsync(cancellationToken);

        if (availableFiles.Count == 0)
        {
            return new EventLogReadResult(
                FileName: string.Empty,
                Entries: Array.Empty<EventLogEntry>(),
                HasMoreEntries: false);
        }

        // El nombre recibido se resuelve contra el listado de archivos existentes.
        // Nunca se combina con la ruta directamente: eso permitiria salir del
        // directorio de registros con una ruta relativa.
        EventLogFileDescriptor selectedFile =
            availableFiles.FirstOrDefault(file =>
                string.Equals(file.FileName, fileName, StringComparison.OrdinalIgnoreCase))
            ?? availableFiles.First();

        string directoryPath = pathResolver.ResolveAbsolutePath(options.DirectoryPath);
        string filePath = Path.Combine(directoryPath, selectedFile.FileName);

        (List<string> lines, bool hasMoreLines) = await ReadLastLinesAsync(
            filePath,
            maximumEntries,
            cancellationToken);

        List<EventLogEntry> entries = new(lines.Count);
        int malformedLineCount = 0;

        // Se recorren al reves para devolver primero las entradas mas recientes.
        for (int index = lines.Count - 1; index >= 0; index--)
        {
            EventLogEntry? entry = ParseEntry(lines[index]);

            if (entry is null)
            {
                malformedLineCount++;
                continue;
            }

            entries.Add(entry);
        }

        if (malformedLineCount > 0)
        {
            logger.LogDebug(
                "Se ignoraron {MalformedLineCount} lineas no interpretables de {FileName}.",
                malformedLineCount,
                selectedFile.FileName);
        }

        return new EventLogReadResult(selectedFile.FileName, entries, hasMoreLines);
    }

    /// <summary>
    /// Lee las ultimas lineas de un archivo sin cargarlo completo en memoria.
    /// </summary>
    /// <remarks>
    /// Se recorre el archivo secuencialmente conservando solo una ventana de las
    /// ultimas <paramref name="maximumLines"/> lineas. Es la forma mas simple de
    /// acotar el consumo de memoria a la cantidad que realmente se va a devolver,
    /// independientemente del tamano del archivo.
    /// </remarks>
    /// <param name="filePath">Ruta del archivo a leer.</param>
    /// <param name="maximumLines">Cantidad maxima de lineas a conservar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Las ultimas lineas y si el archivo tenia mas.</returns>
    private static async Task<(List<string> Lines, bool HasMoreLines)> ReadLastLinesAsync(
        string filePath,
        int maximumLines,
        CancellationToken cancellationToken)
    {
        Queue<string> window = new(maximumLines);
        bool hasMoreLines = false;

        await using FileStream fileStream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            // El archivo lo esta escribiendo Serilog en este momento: se abre
            // permitiendo escritura concurrente para no bloquear el registro.
            FileShare.ReadWrite | FileShare.Delete,
            READ_BUFFER_SIZE_IN_BYTES,
            useAsync: true);

        using StreamReader reader = new(fileStream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            window.Enqueue(line);

            if (window.Count > maximumLines)
            {
                window.Dequeue();
                hasMoreLines = true;
            }
        }

        return (window.ToList(), hasMoreLines);
    }

    private static EventLogEntry? ParseEntry(string line)
    {
        try
        {
            CompactLogRecord? record = JsonSerializer.Deserialize<CompactLogRecord>(
                line,
                JSON_OPTIONS);

            if (record is null)
            {
                return null;
            }

            return new EventLogEntry(
                Timestamp: record.Timestamp ?? DateTimeOffset.MinValue,
                Level: string.IsNullOrWhiteSpace(record.Level) ? "Information" : record.Level,
                Message: record.RenderedMessage ?? record.MessageTemplate ?? string.Empty,
                CorrelationId: record.CorrelationId,
                UserName: record.UserName,
                SourceContext: record.SourceContext,
                Exception: record.Exception);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Forma del registro que escribe el formateador JSON compacto de Serilog.
    /// Los nombres cortos (<c>@t</c>, <c>@l</c>) son los que produce el
    /// formateador, no una abreviatura propia.
    /// </summary>
    private sealed class CompactLogRecord
    {
        [System.Text.Json.Serialization.JsonPropertyName("@t")]
        public DateTimeOffset? Timestamp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("@l")]
        public string? Level { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("@m")]
        public string? RenderedMessage { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("@mt")]
        public string? MessageTemplate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("@x")]
        public string? Exception { get; set; }

        public string? CorrelationId { get; set; }

        public string? UserName { get; set; }

        public string? SourceContext { get; set; }
    }
}
