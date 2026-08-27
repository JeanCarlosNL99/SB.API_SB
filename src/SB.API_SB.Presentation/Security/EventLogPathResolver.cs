using SB.API_SB.Infrastructure.EventLog;

namespace SB.API_SB.Presentation.Security;

/// <summary>
/// Resuelve el directorio de registros contra el directorio raiz de contenido del
/// proyecto de la API, igual que el resolutor de la base de datos de texto plano.
/// </summary>
public sealed class EventLogPathResolver : IEventLogPathResolver
{
    private readonly string contentRootPath;

    public EventLogPathResolver(IHostEnvironment hostEnvironment)
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
