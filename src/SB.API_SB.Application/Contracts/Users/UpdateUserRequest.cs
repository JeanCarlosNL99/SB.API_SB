namespace SB.API_SB.Application.Contracts.Users;

/// <summary>Datos modificables de un usuario existente.</summary>
public sealed class UpdateUserRequest
{
    /// <summary>Correo electronico del usuario.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Nombre completo del usuario.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Indica si el usuario puede iniciar sesion.</summary>
    public bool IsActive { get; set; }

    /// <summary>Identificadores de los roles asignados.</summary>
    public IReadOnlyCollection<Guid> RoleIdentifiers { get; set; } = Array.Empty<Guid>();
}
