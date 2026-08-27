namespace SB.API_SB.Application.Contracts.EventLog;

/// <summary>Archivo de registro de eventos disponible para consulta.</summary>
public sealed class EventLogFileResponse
{
    /// <summary>Nombre del archivo, sin la ruta.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Tamano del archivo en bytes.</summary>
    public long SizeInBytes { get; set; }

    /// <summary>Fecha y hora (UTC) de la ultima escritura.</summary>
    public DateTime LastWriteAtUtc { get; set; }
}

/// <summary>Filtros de la consulta del registro de eventos.</summary>
public sealed class EventLogFilterRequest
{
    /// <summary>Cantidad de entradas devueltas por omision.</summary>
    public const int DEFAULT_MAXIMUM_ENTRIES = 200;

    /// <summary>Cantidad maxima de entradas que se permite solicitar.</summary>
    public const int LIMIT_MAXIMUM_ENTRIES = 1_000;

    /// <summary>
    /// Archivo a leer. Si se omite, se lee el mas reciente. Solo se admiten
    /// nombres presentes en el listado de archivos disponibles.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>Nivel minimo a incluir: Verbose, Debug, Information, Warning, Error o Fatal.</summary>
    public string? MinimumLevel { get; set; }

    /// <summary>Texto a buscar en el mensaje o en la excepcion.</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Cantidad maxima de entradas a devolver.</summary>
    public int MaximumEntries { get; set; } = DEFAULT_MAXIMUM_ENTRIES;
}

/// <summary>Entrada individual del registro de eventos.</summary>
public sealed class EventLogEntryResponse
{
    /// <summary>Fecha y hora en que se registro el evento.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Nivel del evento.</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>Mensaje ya renderizado.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Identificador de correlacion de la peticion, cuando aplica.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Usuario asociado al evento, cuando aplica.</summary>
    public string? UserName { get; set; }

    /// <summary>Origen del evento (clase que lo registro).</summary>
    public string? SourceContext { get; set; }

    /// <summary>Traza de la excepcion, cuando el evento la incluye.</summary>
    public string? Exception { get; set; }
}

/// <summary>Resultado de la consulta del registro de eventos.</summary>
public sealed class EventLogResponse
{
    /// <summary>Archivo efectivamente leido.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Cantidad de entradas devueltas.</summary>
    public int EntryCount { get; set; }

    /// <summary>
    /// Indica si el archivo tiene mas entradas de las devueltas. Sirve para
    /// advertir que el resultado esta recortado y no es el historico completo.
    /// </summary>
    public bool HasMoreEntries { get; set; }

    /// <summary>Cantidad de entradas por nivel, sobre las entradas devueltas.</summary>
    public IReadOnlyDictionary<string, int> CountsByLevel { get; set; } =
        new Dictionary<string, int>();

    /// <summary>Entradas ordenadas de la mas reciente a la mas antigua.</summary>
    public IReadOnlyCollection<EventLogEntryResponse> Entries { get; set; } =
        Array.Empty<EventLogEntryResponse>();
}
