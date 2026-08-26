using SB.API_SB.Infrastructure.FlatFileStorage;

namespace SB.API_SB.Tests.TestDoubles;

/// <summary>
/// Resolutor de rutas que apunta a un directorio temporal. Permite ejercitar el
/// repositorio de texto plano sin depender del proyecto de la API ni ensuciar el
/// repositorio de codigo.
/// </summary>
public sealed class TemporaryDirectoryPathResolver : IFlatFilePathResolver
{
    private readonly string basePath;

    public TemporaryDirectoryPathResolver(string basePath)
    {
        this.basePath = basePath;
    }

    /// <inheritdoc />
    public string ResolveAbsolutePath(string relativePath) =>
        Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(basePath, relativePath);
}
