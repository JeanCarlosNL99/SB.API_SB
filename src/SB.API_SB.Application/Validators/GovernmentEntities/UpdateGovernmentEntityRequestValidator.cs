using FluentValidation;
using SB.API_SB.Application.Contracts.GovernmentEntities;

namespace SB.API_SB.Application.Validators.GovernmentEntities;

/// <summary>Validaciones de la actualizacion de una entidad gubernamental.</summary>
public sealed class UpdateGovernmentEntityRequestValidator
    : AbstractValidator<UpdateGovernmentEntityRequest>
{
    public UpdateGovernmentEntityRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre de la entidad es obligatorio.")
            .MaximumLength(ValidationLimits.ENTITY_NAME_MAXIMUM_LENGTH)
            .WithMessage($"El nombre no puede exceder {ValidationLimits.ENTITY_NAME_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.Category)
            .NotEmpty().WithMessage("La categoria de la entidad es obligatoria.")
            .MaximumLength(ValidationLimits.CLASSIFICATION_MAXIMUM_LENGTH)
            .WithMessage($"La categoria no puede exceder {ValidationLimits.CLASSIFICATION_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.StateBranch)
            .NotEmpty().WithMessage("El poder del Estado es obligatorio.")
            .MaximumLength(ValidationLimits.CLASSIFICATION_MAXIMUM_LENGTH)
            .WithMessage($"El poder del Estado no puede exceder {ValidationLimits.CLASSIFICATION_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.Sector)
            .NotEmpty().WithMessage("El sector es obligatorio.")
            .MaximumLength(ValidationLimits.CLASSIFICATION_MAXIMUM_LENGTH)
            .WithMessage($"El sector no puede exceder {ValidationLimits.CLASSIFICATION_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.Status)
            .IsInEnum().WithMessage("El estado indicado no es valido.");
    }
}
