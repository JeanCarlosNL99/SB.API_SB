using SB.API_SB.Application.Contracts.Users;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Application.Mappings;

/// <summary>Proyecciones de usuarios y roles hacia sus contratos publicos.</summary>
public static class UserMappings
{
    /// <summary>
    /// Convierte un usuario en su respuesta de API, excluyendo deliberadamente el
    /// hash y la sal de la contrasena.
    /// </summary>
    /// <param name="user">Usuario de dominio.</param>
    /// <returns>Respuesta lista para devolverse desde la API.</returns>
    public static UserResponse ToResponse(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles
                .Where(userRole => userRole.Role is not null)
                .Select(userRole => userRole.Role!.ToResponse())
                .ToList()
        };
    }

    /// <summary>Convierte un rol en su respuesta de API.</summary>
    /// <param name="role">Rol de dominio.</param>
    /// <returns>Respuesta lista para devolverse desde la API.</returns>
    public static RoleResponse ToResponse(this Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        };
    }
}
