namespace SB.API_SB.Infrastructure.FlatFileStorage;

/// <summary>
/// Resuelve a ruta absoluta las rutas relativas configuradas para la base de
/// datos de texto plano. La capa de presentacion es la que conoce el directorio
/// raiz del contenido, por eso la resolucion se abstrae detras de un contrato.
/// </summary>
public interface IFlatFilePathResolver
{
    /// <summary>Convierte una ruta relativa al proyecto en una ruta absoluta.</summary>
    /// <param name="relativePath">Ruta relativa configurada.</param>
    /// <returns>Ruta absoluta en el sistema de archivos.</returns>
    string ResolveAbsolutePath(string relativePath);
}
