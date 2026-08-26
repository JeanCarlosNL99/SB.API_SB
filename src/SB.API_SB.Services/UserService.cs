using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Contracts.Users;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Application.Mappings;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Services;

/// <summary>Implementacion de la gestion de usuarios y asignacion de roles.</summary>
public sealed class UserService : IUserService
{
    private const string USER_ENTITY_NAME = "el usuario";
    private const string USER_NAME_FIELD_NAME = "nombre de usuario o correo";

    private readonly IUserRepository userRepository;
    private readonly IRoleRepository roleRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<UserService> logger;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
    {
        this.userRepository = userRepository;
        this.roleRepository = roleRepository;
        this.passwordHasher = passwordHasher;
        this.dateTimeProvider = dateTimeProvider;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<UserResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<User> users = await userRepository.GetAllWithRolesAsync(
            cancellationToken);

        return users.Select(user => user.ToResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<UserResponse> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User user = await GetRequiredUserAsync(userId, cancellationToken);

        return user.ToResponse();
    }

    /// <inheritdoc />
    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string normalizedUserName = request.UserName.Trim();
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        bool userAlreadyExists = await userRepository.ExistsByUserNameOrEmailAsync(
            normalizedUserName,
            normalizedEmail,
            excludedUserId: null,
            cancellationToken);

        if (userAlreadyExists)
        {
            throw new DuplicatedEntityException(
                USER_ENTITY_NAME,
                USER_NAME_FIELD_NAME,
                normalizedUserName);
        }

        IReadOnlyCollection<Role> roles = await GetRequiredRolesAsync(
            request.RoleIdentifiers,
            cancellationToken);

        (string hash, string salt) = passwordHasher.HashPassword(request.Password);

        User user = new()
        {
            UserName = normalizedUserName,
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true
        };

        AssignRoles(user, roles);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Usuario {UserId} creado con {RoleCount} rol(es).",
            user.Id,
            roles.Count);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserResponse> UpdateAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User user = await GetRequiredUserAsync(userId, cancellationToken);

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        bool emailAlreadyExists = await userRepository.ExistsByUserNameOrEmailAsync(
            user.UserName,
            normalizedEmail,
            userId,
            cancellationToken);

        if (emailAlreadyExists)
        {
            throw new DuplicatedEntityException(USER_ENTITY_NAME, "correo", normalizedEmail);
        }

        IReadOnlyCollection<Role> roles = await GetRequiredRolesAsync(
            request.RoleIdentifiers,
            cancellationToken);

        user.Email = normalizedEmail;
        user.FullName = request.FullName.Trim();
        user.IsActive = request.IsActive;

        user.UserRoles.Clear();
        AssignRoles(user, roles);

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Usuario {UserId} actualizado.", user.Id);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User user = await GetRequiredUserAsync(userId, cancellationToken);

        bool currentPasswordIsValid = passwordHasher.VerifyPassword(
            request.CurrentPassword,
            user.PasswordHash,
            user.PasswordSalt);

        if (!currentPasswordIsValid)
        {
            logger.LogWarning(
                "Cambio de contrasena rechazado para el usuario {UserId}: contrasena actual incorrecta.",
                userId);

            throw new InvalidCredentialsException();
        }

        (string hash, string salt) = passwordHasher.HashPassword(request.NewPassword);

        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Contrasena del usuario {UserId} actualizada.", userId);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User user = await GetRequiredUserAsync(userId, cancellationToken);

        await userRepository.DeleteAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Usuario {UserId} eliminado.", userId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Role> roles = await roleRepository.GetAllAsync(cancellationToken);

        return roles.Select(role => role.ToResponse()).ToList();
    }

    private void AssignRoles(User user, IReadOnlyCollection<Role> roles)
    {
        DateTime assignedAt = dateTimeProvider.UtcNow;

        foreach (Role role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = assignedAt
            });
        }
    }

    private async Task<User> GetRequiredUserAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await userRepository.GetByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(USER_ENTITY_NAME, userId);

    private async Task<IReadOnlyCollection<Role>> GetRequiredRolesAsync(
        IReadOnlyCollection<Guid> roleIdentifiers,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Role> roles = await roleRepository.GetByIdentifiersAsync(
            roleIdentifiers,
            cancellationToken);

        if (roles.Count != roleIdentifiers.Distinct().Count())
        {
            throw new BusinessRuleViolationException(
                "Uno o mas de los roles indicados no existen en el sistema.");
        }

        return roles;
    }
}
