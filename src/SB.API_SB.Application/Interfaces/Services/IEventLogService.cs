using SB.API_SB.Application.Contracts.EventLog;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>
/// Consulta del registro de eventos de la aplicacion.
/// </summary>
/// <remarks>
/// Expone los eventos que escribe Serilog para que un administrador pueda
/// revisarlos desde la propia aplicacion, sin acceso al servidor de archivos.
/// </remarks>
public interface IEventLogService
{
    /// <summary>Obtiene los archivos de registro disponibles.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Archivos ordenados del mas reciente al mas antiguo.</returns>
    Task<IReadOnlyCollection<EventLogFileResponse>> GetFilesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Lee las entradas del registro aplicando los filtros indicados.</summary>
    /// <param name="filter">Archivo, nivel minimo, texto y cantidad maxima.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Entradas encontradas, de la mas reciente a la mas antigua.</returns>
    Task<EventLogResponse> ReadAsync(
        EventLogFilterRequest filter,
        CancellationToken cancellationToken = default);
}
