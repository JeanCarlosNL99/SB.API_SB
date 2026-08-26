using SB.API_SB.Domain.Common;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Usuario que se autentica contra la API. La contrasena nunca se almacena en
/// texto claro: solo se conserva su hash y la sal utilizada.
/// </summary>
public sealed class User : AuditableEntity
{
    /// <summary>Nombre de usuario con el que inicia sesion.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Correo electronico del usuario.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Nombre completo del usuario.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Hash de la contrasena en formato Base64.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Sal aleatoria utilizada para derivar el hash, en formato Base64.</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>Indica si el usuario puede iniciar sesion.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Fecha y hora (UTC) del ultimo inicio de sesion exitoso.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Roles asignados al usuario.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>Nombres de los roles asignados, listos para emitirse como claims.</summary>
    public IReadOnlyCollection<string> GetRoleNames() =>
        UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => userRole.Role!.Name)
            .Distinct()
            .ToList();
}
