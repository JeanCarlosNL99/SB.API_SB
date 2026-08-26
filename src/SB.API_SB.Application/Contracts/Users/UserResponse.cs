namespace SB.API_SB.Application.Contracts.Users;

/// <summary>
/// Usuario expuesto por la API. No incluye el hash ni la sal de la contrasena:
/// el DTO existe precisamente para no filtrar datos sensibles de la entidad.
/// </summary>
public sealed class UserResponse
{
    /// <summary>Identificador del usuario.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre de usuario.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Correo electronico.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Nombre completo.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Indica si el usuario puede iniciar sesion.</summary>
    public bool IsActive { get; set; }

    /// <summary>Fecha y hora (UTC) del ultimo inicio de sesion.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Fecha y hora (UTC) de creacion del registro.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Roles asignados al usuario.</summary>
    public IReadOnlyCollection<RoleResponse> Roles { get; set; } = Array.Empty<RoleResponse>();
}
