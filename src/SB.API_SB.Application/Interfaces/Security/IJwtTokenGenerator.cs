using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Application.Interfaces.Security;

/// <summary>Contrato de emision de tokens de acceso JWT.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Genera un token de acceso firmado para el usuario indicado.</summary>
    /// <param name="user">Usuario autenticado, con sus roles cargados.</param>
    /// <returns>Token generado y su fecha de expiracion en UTC.</returns>
    (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(User user);
}
