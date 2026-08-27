using FluentValidation;
using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Application.Validators.Payroll;

/// <summary>
/// Validaciones de la solicitud de generacion de nomina.
/// </summary>
/// <remarks>
/// Aqui solo se comprueba que el ano y la semana esten dentro de rangos
/// razonables. Que la semana exista realmente en el calendario y que no haya sido
/// pagada son reglas de negocio y las verifica el dominio.
/// </remarks>
public sealed class GeneratePayrollRunRequestValidator
    : AbstractValidator<GeneratePayrollRunRequest>
{
    public GeneratePayrollRunRequestValidator()
    {
        RuleFor(request => request.CompanyId)
            .NotEqual(Guid.Empty).WithMessage("La compania es obligatoria.");

        RuleFor(request => request.Year)
            .InclusiveBetween(PayrollWeek.MINIMUM_YEAR, PayrollWeek.MAXIMUM_YEAR)
            .WithMessage($"El ano debe estar entre {PayrollWeek.MINIMUM_YEAR} y {PayrollWeek.MAXIMUM_YEAR}.");

        RuleFor(request => request.WeekNumber)
            .InclusiveBetween(PayrollWeek.FIRST_WEEK_NUMBER, PayrollWeek.LAST_POSSIBLE_WEEK_NUMBER)
            .WithMessage($"El numero de semana debe estar entre {PayrollWeek.FIRST_WEEK_NUMBER} y {PayrollWeek.LAST_POSSIBLE_WEEK_NUMBER}.");
    }
}
