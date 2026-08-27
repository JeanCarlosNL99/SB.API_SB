namespace SB.API_SB.Infrastructure.Options;

/// <summary>
/// Configuracion de la consulta del registro de eventos desde la aplicacion.
/// </summary>
public sealed class EventLogOptions
{
    /// <summary>Nombre de la seccion de configuracion asociada.</summary>
    public const string SECTION_NAME = "EventLog";

    /// <summary>
    /// Directorio donde Serilog escribe los archivos, relativo al directorio raiz
    /// del proyecto de la API.
    /// </summary>
    public string DirectoryPath { get; set; } = "Logs";
}
