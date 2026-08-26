namespace SB.API_SB.Application.Contracts.Users;

/// <summary>Datos necesarios para cambiar la contrasena de un usuario.</summary>
public sealed class ChangePasswordRequest
{
    /// <summary>Contrasena actual del usuario.</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>Nueva contrasena deseada.</summary>
    public string NewPassword { get; set; } = string.Empty;
}
