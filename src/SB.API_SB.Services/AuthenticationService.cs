using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Contracts.Authentication;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Services;

/// <summary>
/// Implementacion de la autenticacion por usuario y contrasena con emision de
/// token JWT.
/// </summary>
/// <remarks>
/// Todos los fallos devuelven la misma excepcion generica, sin distinguir entre
/// usuario inexistente, contrasena incorrecta o usuario inactivo: revelar la
/// causa exacta permitiria enumerar usuarios validos. El log interno si registra
/// el motivo real para poder investigar los intentos fallidos.
/// </remarks>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IJwtTokenGenerator tokenGenerator;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<AuthenticationService> logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<AuthenticationService> logger)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.tokenGenerator = tokenGenerator;
        this.dateTimeProvider = dateTimeProvider;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string normalizedUserName = request.UserName.Trim();

        User? user = await userRepository.GetByUserNameWithRolesAsync(
            normalizedUserName,
            cancellationToken);

        if (user is null)
        {
            logger.LogWarning(
                "Intento de inicio de sesion fallido: el usuario {UserName} no existe.",
                normalizedUserName);

            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            logger.LogWarning(
                "Intento de inicio de sesion fallido: el usuario {UserName} esta inactivo.",
                normalizedUserName);

            throw new InvalidCredentialsException();
        }

        bool passwordIsValid = passwordHasher.VerifyPassword(
            request.Password,
            user.PasswordHash,
            user.PasswordSalt);

        if (!passwordIsValid)
        {
            logger.LogWarning(
                "Intento de inicio de sesion fallido: contrasena incorrecta para {UserName}.",
                normalizedUserName);

            throw new InvalidCredentialsException();
        }

        (string accessToken, DateTime expiresAtUtc) = tokenGenerator.GenerateAccessToken(user);

        user.LastLoginAt = dateTimeProvider.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Inicio de sesion exitoso del usuario {UserName}. El token expira en {ExpiresAtUtc}.",
            user.UserName,
            expiresAtUtc);

        return new AuthenticationResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Roles = user.GetRoleNames()
        };
    }
}
