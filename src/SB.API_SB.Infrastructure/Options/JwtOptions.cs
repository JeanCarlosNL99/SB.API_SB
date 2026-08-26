namespace SB.API_SB.Infrastructure.Options;

/// <summary>
/// Configuracion del token JWT. La clave de firma se lee de AppSettings.json (o
/// de variables de entorno y secretos de usuario en produccion), nunca del
/// codigo fuente.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Nombre de la seccion de configuracion asociada.</summary>
    public const string SECTION_NAME = "Jwt";

    /// <summary>Emisor del token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audiencia para la que se emite el token.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Clave simetrica utilizada para firmar el token.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Minutos de vigencia del token de acceso.</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>Tolerancia en segundos aplicada al validar la expiracion.</summary>
    public int ClockSkewSeconds { get; set; } = 30;
}
