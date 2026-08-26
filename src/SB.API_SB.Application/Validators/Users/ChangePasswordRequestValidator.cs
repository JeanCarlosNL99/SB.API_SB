using FluentValidation;
using SB.API_SB.Application.Contracts.Users;

namespace SB.API_SB.Application.Validators.Users;

/// <summary>Validaciones del cambio de contrasena.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty().WithMessage("La contrasena actual es obligatoria.");

        RuleFor(request => request.NewPassword).ApplyPasswordPolicy();

        RuleFor(request => request.NewPassword)
            .NotEqual(request => request.CurrentPassword)
            .WithMessage("La nueva contrasena debe ser distinta de la actual.");
    }
}
