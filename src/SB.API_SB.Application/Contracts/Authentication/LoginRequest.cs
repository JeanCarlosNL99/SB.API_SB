namespace SB.API_SB.Application.Contracts.Authentication;

/// <summary>Credenciales enviadas para iniciar sesion.</summary>
public sealed class LoginRequest
{
    /// <summary>Nombre de usuario.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Contrasena en texto claro. Solo viaja sobre HTTPS.</summary>
    public string Password { get; set; } = string.Empty;
}
