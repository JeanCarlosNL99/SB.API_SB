using FluentValidation;
using SB.API_SB.Application.Contracts.Users;

namespace SB.API_SB.Application.Validators.Users;

/// <summary>Validaciones del alta de un usuario.</summary>
public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MinimumLength(ValidationLimits.USER_NAME_MINIMUM_LENGTH)
            .WithMessage($"El nombre de usuario debe tener al menos {ValidationLimits.USER_NAME_MINIMUM_LENGTH} caracteres.")
            .MaximumLength(ValidationLimits.USER_NAME_MAXIMUM_LENGTH)
            .WithMessage($"El nombre de usuario no puede exceder {ValidationLimits.USER_NAME_MAXIMUM_LENGTH} caracteres.")
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("El nombre de usuario solo admite letras, numeros, puntos, guiones y guiones bajos.");

        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("El correo electronico es obligatorio.")
            .EmailAddress().WithMessage("El correo electronico no tiene un formato valido.")
            .MaximumLength(ValidationLimits.EMAIL_MAXIMUM_LENGTH)
            .WithMessage($"El correo electronico no puede exceder {ValidationLimits.EMAIL_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.FullName)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(ValidationLimits.PERSON_NAME_MAXIMUM_LENGTH)
            .WithMessage($"El nombre completo no puede exceder {ValidationLimits.PERSON_NAME_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.Password).ApplyPasswordPolicy();

        RuleFor(request => request.RoleIdentifiers)
            .NotEmpty().WithMessage("Debe asignarse al menos un rol al usuario.");

        RuleForEach(request => request.RoleIdentifiers)
            .NotEqual(Guid.Empty).WithMessage("Los roles asignados deben ser identificadores validos.");
    }
}
