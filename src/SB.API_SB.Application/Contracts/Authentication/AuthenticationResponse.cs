namespace SB.API_SB.Application.Contracts.Authentication;

/// <summary>Resultado de una autenticacion exitosa.</summary>
public sealed class AuthenticationResponse
{
    /// <summary>Token de acceso a enviar en el encabezado Authorization Bearer.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Tipo de token emitido.</summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>Fecha y hora (UTC) en que expira el token.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Identificador del usuario autenticado.</summary>
    public Guid UserId { get; set; }

    /// <summary>Nombre de usuario autenticado.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Nombre completo del usuario autenticado.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Roles asignados al usuario.</summary>
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
