using FluentValidation;
using SB.API_SB.Application.Contracts.Companies;

namespace SB.API_SB.Application.Validators.Companies;

/// <summary>Validaciones de la actualizacion de una compania.</summary>
public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("La razon social es obligatoria.")
            .MaximumLength(ValidationLimits.COMPANY_NAME_MAXIMUM_LENGTH)
            .WithMessage($"La razon social no puede exceder {ValidationLimits.COMPANY_NAME_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.TaxIdentificationNumber)
            .NotEmpty().WithMessage("El Registro Nacional de Contribuyente es obligatorio.")
            .MaximumLength(ValidationLimits.TAX_IDENTIFICATION_MAXIMUM_LENGTH)
            .WithMessage($"El registro no puede exceder {ValidationLimits.TAX_IDENTIFICATION_MAXIMUM_LENGTH} caracteres.")
            .Matches("^[0-9A-Za-z-]+$")
            .WithMessage("El registro solo admite letras, numeros y guiones.");
    }
}
