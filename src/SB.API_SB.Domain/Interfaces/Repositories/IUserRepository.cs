using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>Operaciones de persistencia especificas de usuarios.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Obtiene un usuario por su nombre de usuario, incluyendo sus roles.</summary>
    /// <param name="userName">Nombre de usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario con sus roles o nulo.</returns>
    Task<User?> GetByUserNameWithRolesAsync(
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene un usuario por su identificador, incluyendo sus roles.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario con sus roles o nulo.</returns>
    Task<User?> GetByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene todos los usuarios con sus roles.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Coleccion de usuarios con roles.</returns>
    Task<IReadOnlyCollection<User>> GetAllWithRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Determina si el nombre de usuario o el correo ya estan registrados.</summary>
    /// <param name="userName">Nombre de usuario a verificar.</param>
    /// <param name="email">Correo a verificar.</param>
    /// <param name="excludedUserId">Usuario a excluir de la verificacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Verdadero si ya existe un usuario con esos datos.</returns>
    Task<bool> ExistsByUserNameOrEmailAsync(
        string userName,
        string email,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default);
}
