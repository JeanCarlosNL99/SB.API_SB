using SB.API_SB.Application.Contracts.Users;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>Casos de uso de la gestion de usuarios y sus roles.</summary>
public interface IUserService
{
    /// <summary>Obtiene todos los usuarios con sus roles.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Usuarios registrados.</returns>
    Task<IReadOnlyCollection<UserResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene un usuario por su identificador.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario solicitado.</returns>
    Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Registra un nuevo usuario y le asigna sus roles.</summary>
    /// <param name="request">Datos del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario registrado.</returns>
    Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Actualiza los datos y los roles de un usuario.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="request">Nuevos datos del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El usuario actualizado.</returns>
    Task<UserResponse> UpdateAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Cambia la contrasena de un usuario.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="request">Contrasena actual y nueva.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Elimina un usuario.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene los roles disponibles en el sistema.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Roles registrados.</returns>
    Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(
        CancellationToken cancellationToken = default);
}
