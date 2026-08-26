namespace SB.API_SB.Application.Contracts.Users;

/// <summary>Datos necesarios para registrar un nuevo usuario.</summary>
public sealed class CreateUserRequest
{
    /// <summary>Nombre de usuario con el que iniciara sesion.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Correo electronico del usuario.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Nombre completo del usuario.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Contrasena inicial en texto claro.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Identificadores de los roles a asignar.</summary>
    public IReadOnlyCollection<Guid> RoleIdentifiers { get; set; } = Array.Empty<Guid>();
}
