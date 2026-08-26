using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>Operaciones de persistencia especificas de roles.</summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>Obtiene los roles cuyos identificadores se indican.</summary>
    /// <param name="roleIdentifiers">Identificadores de los roles solicitados.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Roles encontrados.</returns>
    Task<IReadOnlyCollection<Role>> GetByIdentifiersAsync(
        IReadOnlyCollection<Guid> roleIdentifiers,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene un rol por su nombre.</summary>
    /// <param name="name">Nombre del rol.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El rol encontrado o nulo.</returns>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
