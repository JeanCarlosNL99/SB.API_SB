using FluentValidation;
using SB.API_SB.Application.Contracts.Authentication;

namespace SB.API_SB.Application.Validators.Authentication;

/// <summary>Validaciones de las credenciales de inicio de sesion.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MaximumLength(ValidationLimits.USER_NAME_MAXIMUM_LENGTH)
            .WithMessage($"El nombre de usuario no puede exceder {ValidationLimits.USER_NAME_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("La contrasena es obligatoria.")
            .MaximumLength(ValidationLimits.PASSWORD_MAXIMUM_LENGTH)
            .WithMessage($"La contrasena no puede exceder {ValidationLimits.PASSWORD_MAXIMUM_LENGTH} caracteres.");
    }
}
