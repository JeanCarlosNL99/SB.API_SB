using FluentValidation;
using SB.API_SB.Application.Contracts.Departments;

namespace SB.API_SB.Application.Validators.Departments;

/// <summary>Validaciones de la actualizacion de un departamento.</summary>
public sealed class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre del departamento es obligatorio.")
            .MaximumLength(ValidationLimits.DEPARTMENT_NAME_MAXIMUM_LENGTH)
            .WithMessage($"El nombre no puede exceder {ValidationLimits.DEPARTMENT_NAME_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.Code)
            .NotEmpty().WithMessage("El codigo del departamento es obligatorio.")
            .MaximumLength(ValidationLimits.DEPARTMENT_CODE_MAXIMUM_LENGTH)
            .WithMessage($"El codigo no puede exceder {ValidationLimits.DEPARTMENT_CODE_MAXIMUM_LENGTH} caracteres.");
    }
}
