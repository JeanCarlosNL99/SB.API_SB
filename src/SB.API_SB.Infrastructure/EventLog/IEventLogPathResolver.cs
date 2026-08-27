namespace SB.API_SB.Infrastructure.EventLog;

/// <summary>
/// Resuelve a ruta absoluta el directorio configurado para los archivos de
/// registro. Como el resolutor de la base de datos de texto plano, existe porque
/// solo la capa de Presentacion conoce el directorio raiz del contenido.
/// </summary>
public interface IEventLogPathResolver
{
    /// <summary>Convierte una ruta relativa al proyecto en una ruta absoluta.</summary>
    /// <param name="relativePath">Ruta relativa configurada.</param>
    /// <returns>Ruta absoluta en el sistema de archivos.</returns>
    string ResolveAbsolutePath(string relativePath);
}
