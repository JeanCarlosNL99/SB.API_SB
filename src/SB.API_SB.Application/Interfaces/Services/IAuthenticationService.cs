using SB.API_SB.Application.Contracts.Authentication;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>Caso de uso de autenticacion de usuarios.</summary>
public interface IAuthenticationService
{
    /// <summary>Valida las credenciales y emite un token de acceso JWT.</summary>
    /// <param name="request">Credenciales del usuario.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Token de acceso y datos basicos del usuario autenticado.</returns>
    Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
