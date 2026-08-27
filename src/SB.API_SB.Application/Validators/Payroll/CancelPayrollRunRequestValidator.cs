using FluentValidation;
using SB.API_SB.Application.Contracts.Payroll;

namespace SB.API_SB.Application.Validators.Payroll;

/// <summary>
/// Validaciones de la anulacion de una nomina. Se exige un motivo con contenido
/// real porque la anulacion queda como evidencia en el historico.
/// </summary>
public sealed class CancelPayrollRunRequestValidator : AbstractValidator<CancelPayrollRunRequest>
{
    public CancelPayrollRunRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty().WithMessage("Debe indicar el motivo de la anulacion.")
            .MinimumLength(ValidationLimits.CANCELLATION_REASON_MINIMUM_LENGTH)
            .WithMessage($"El motivo debe tener al menos {ValidationLimits.CANCELLATION_REASON_MINIMUM_LENGTH} caracteres.")
            .MaximumLength(ValidationLimits.CANCELLATION_REASON_MAXIMUM_LENGTH)
            .WithMessage($"El motivo no puede exceder {ValidationLimits.CANCELLATION_REASON_MAXIMUM_LENGTH} caracteres.");
    }
}
