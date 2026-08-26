using SB.API_SB.Infrastructure.FlatFileStorage;

namespace SB.API_SB.Presentation.Security;

/// <summary>
/// Resuelve las rutas de la base de datos de texto plano contra el directorio
/// raiz de contenido del proyecto de la API.
/// </summary>
/// <remarks>
/// Vive en la capa de Presentacion porque es la unica que conoce
/// <see cref="IHostEnvironment.ContentRootPath"/>. La Infraestructura solo
/// depende de la abstraccion, lo que ademas permite apuntar a un directorio
/// temporal en las pruebas.
/// </remarks>
public sealed class FlatFilePathResolver : IFlatFilePathResolver
{
    private readonly string contentRootPath;

    public FlatFilePathResolver(IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        contentRootPath = hostEnvironment.ContentRootPath;
    }

    /// <inheritdoc />
    public string ResolveAbsolutePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        return Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.GetFullPath(Path.Combine(contentRootPath, relativePath));
    }
}
