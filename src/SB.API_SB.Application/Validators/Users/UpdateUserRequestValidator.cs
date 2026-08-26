using FluentValidation;
using SB.API_SB.Application.Contracts.Users;

namespace SB.API_SB.Application.Validators.Users;

/// <summary>Validaciones de la actualizacion de un usuario.</summary>
public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("El correo electronico es obligatorio.")
            .EmailAddress().WithMessage("El correo electronico no tiene un formato valido.")
            .MaximumLength(ValidationLimits.EMAIL_MAXIMUM_LENGTH)
            .WithMessage($"El correo electronico no puede exceder {ValidationLimits.EMAIL_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.FullName)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(ValidationLimits.PERSON_NAME_MAXIMUM_LENGTH)
            .WithMessage($"El nombre completo no puede exceder {ValidationLimits.PERSON_NAME_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.RoleIdentifiers)
            .NotEmpty().WithMessage("Debe asignarse al menos un rol al usuario.");

        RuleForEach(request => request.RoleIdentifiers)
            .NotEqual(Guid.Empty).WithMessage("Los roles asignados deben ser identificadores validos.");
    }
}
