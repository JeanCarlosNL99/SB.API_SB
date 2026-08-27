namespace SB.API_SB.Application.Interfaces.EventLog;

/// <summary>Archivo de registro localizado en el almacenamiento.</summary>
/// <param name="FileName">Nombre del archivo, sin ruta.</param>
/// <param name="SizeInBytes">Tamano en bytes.</param>
/// <param name="LastWriteAtUtc">Fecha y hora de la ultima escritura, en UTC.</param>
public sealed record EventLogFileDescriptor(
    string FileName,
    long SizeInBytes,
    DateTime LastWriteAtUtc);

/// <summary>Entrada leida del registro de eventos.</summary>
/// <param name="Timestamp">Momento en que se registro el evento.</param>
/// <param name="Level">Nivel del evento.</param>
/// <param name="Message">Mensaje ya renderizado.</param>
/// <param name="CorrelationId">Identificador de correlacion de la peticion.</param>
/// <param name="UserName">Usuario asociado al evento.</param>
/// <param name="SourceContext">Clase que registro el evento.</param>
/// <param name="Exception">Traza de la excepcion, cuando aplica.</param>
public sealed record EventLogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? CorrelationId,
    string? UserName,
    string? SourceContext,
    string? Exception);

/// <summary>Resultado de leer un archivo de registro.</summary>
/// <param name="FileName">Archivo efectivamente leido.</param>
/// <param name="Entries">Entradas leidas, de la mas reciente a la mas antigua.</param>
/// <param name="HasMoreEntries">Indica si el archivo contiene mas entradas de las devueltas.</param>
public sealed record EventLogReadResult(
    string FileName,
    IReadOnlyCollection<EventLogEntry> Entries,
    bool HasMoreEntries);

/// <summary>
/// Contrato de lectura del registro de eventos.
/// </summary>
/// <remarks>
/// La capa de Aplicacion declara que necesita leer los eventos registrados; que
/// esten en archivos escritos por Serilog es un detalle de infraestructura. Si
/// manana los eventos se envian a un servicio centralizado, se sustituye la
/// implementacion sin tocar el servicio ni el controlador.
/// </remarks>
public interface IEventLogReader
{
    /// <summary>Enumera los archivos de registro disponibles.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Archivos ordenados del mas reciente al mas antiguo.</returns>
    Task<IReadOnlyCollection<EventLogFileDescriptor>> GetFilesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Lee las ultimas entradas de un archivo de registro.</summary>
    /// <param name="fileName">Archivo a leer, o nulo para leer el mas reciente.</param>
    /// <param name="maximumEntries">Cantidad maxima de entradas a devolver.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Entradas leidas y si el archivo tenia mas.</returns>
    Task<EventLogReadResult> ReadAsync(
        string? fileName,
        int maximumEntries,
        CancellationToken cancellationToken = default);
}
