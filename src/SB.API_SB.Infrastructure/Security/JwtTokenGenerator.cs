using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Security;

/// <summary>
/// Emite tokens de acceso JWT firmados con HMAC-SHA256.
/// </summary>
/// <remarks>
/// El token incluye el identificador y el nombre del usuario y un claim de rol
/// por cada rol asignado, lo que permite que la API autorice por rol sin volver a
/// consultar la base de datos en cada peticion. La clave de firma proviene de la
/// configuracion, nunca del codigo fuente.
/// </remarks>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private const int MINIMUM_SIGNING_KEY_LENGTH = 32;

    private readonly JwtOptionsSnapshot optionsSnapshot;
    private readonly IDateTimeProvider dateTimeProvider;

    public JwtTokenGenerator(
        IOptions<Options.JwtOptions> options,
        IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);

        optionsSnapshot = JwtOptionsSnapshot.FromOptions(options.Value);
        this.dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTime issuedAtUtc = dateTimeProvider.UtcNow;
        DateTime expiresAtUtc = issuedAtUtc.AddMinutes(optionsSnapshot.ExpirationMinutes);

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(optionsSnapshot.SigningKey));
        SigningCredentials signingCredentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken securityToken = new(
            issuer: optionsSnapshot.Issuer,
            audience: optionsSnapshot.Audience,
            claims: BuildClaims(user, issuedAtUtc),
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        string accessToken = new JwtSecurityTokenHandler().WriteToken(securityToken);

        return (accessToken, expiresAtUtc);
    }

    private static IEnumerable<Claim> BuildClaims(User user, DateTime issuedAtUtc)
    {
        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.SUBJECT, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.TOKEN_IDENTIFIER, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.USER_NAME, user.UserName),
            new Claim(JwtRegisteredClaimNames.EMAIL, user.Email),
            new Claim(JwtRegisteredClaimNames.FULL_NAME, user.FullName),
            new Claim(
                JwtRegisteredClaimNames.ISSUED_AT,
                EpochTime.GetIntDate(issuedAtUtc).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        claims.AddRange(user
            .GetRoleNames()
            .Select(roleName => new Claim(ClaimTypes.Role, roleName)));

        return claims;
    }

    /// <summary>
    /// Copia validada de la configuracion del token. Validar una sola vez al
    /// construir el generador evita repetir comprobaciones en cada emision y
    /// falla de inmediato si la aplicacion esta mal configurada.
    /// </summary>
    private sealed record JwtOptionsSnapshot(
        string Issuer,
        string Audience,
        string SigningKey,
        int ExpirationMinutes)
    {
        public static JwtOptionsSnapshot FromOptions(Options.JwtOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SigningKey) ||
                options.SigningKey.Length < MINIMUM_SIGNING_KEY_LENGTH)
            {
                throw new InvalidOperationException(
                    "La clave de firma del token JWT debe estar configurada y tener al menos " +
                    $"{MINIMUM_SIGNING_KEY_LENGTH} caracteres.");
            }

            if (string.IsNullOrWhiteSpace(options.Issuer) ||
                string.IsNullOrWhiteSpace(options.Audience))
            {
                throw new InvalidOperationException(
                    "El emisor y la audiencia del token JWT deben estar configurados.");
            }

            if (options.AccessTokenExpirationMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "La vigencia del token JWT debe ser mayor que cero minutos.");
            }

            return new JwtOptionsSnapshot(
                options.Issuer,
                options.Audience,
                options.SigningKey,
                options.AccessTokenExpirationMinutes);
        }
    }
}

/// <summary>
/// Nombres de los claims emitidos en el token. Se declaran como constantes para
/// que la API y el cliente compartan una unica fuente de verdad.
/// </summary>
public static class JwtRegisteredClaimNames
{
    /// <summary>Identificador del usuario.</summary>
    public const string SUBJECT = "sub";

    /// <summary>Identificador unico del token.</summary>
    public const string TOKEN_IDENTIFIER = "jti";

    /// <summary>Momento de emision del token.</summary>
    public const string ISSUED_AT = "iat";

    /// <summary>Nombre de usuario.</summary>
    public const string USER_NAME = "userName";

    /// <summary>Correo electronico del usuario.</summary>
    public const string EMAIL = "email";

    /// <summary>Nombre completo del usuario.</summary>
    public const string FULL_NAME = "fullName";
}
