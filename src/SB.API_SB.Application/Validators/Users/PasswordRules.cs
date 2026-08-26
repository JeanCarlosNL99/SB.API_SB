using FluentValidation;

namespace SB.API_SB.Application.Validators.Users;

/// <summary>
/// Reglas de complejidad de contrasenas. Se extraen a un metodo de extension
/// para aplicarlas de forma identica en el alta de usuarios y en el cambio de
/// contrasena, sin duplicar los mensajes.
/// </summary>
public static class PasswordRules
{
    private const string UPPERCASE_PATTERN = "[A-Z]";
    private const string LOWERCASE_PATTERN = "[a-z]";
    private const string DIGIT_PATTERN = "[0-9]";

    /// <summary>Aplica las reglas minimas de seguridad a una contrasena.</summary>
    /// <typeparam name="TRequest">Tipo de la solicitud validada.</typeparam>
    /// <param name="ruleBuilder">Constructor de reglas de FluentValidation.</param>
    /// <returns>El constructor de reglas, para permitir encadenamiento.</returns>
    public static IRuleBuilderOptions<TRequest, string> ApplyPasswordPolicy<TRequest>(
        this IRuleBuilder<TRequest, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("La contrasena es obligatoria.")
            .MinimumLength(ValidationLimits.PASSWORD_MINIMUM_LENGTH)
            .WithMessage($"La contrasena debe tener al menos {ValidationLimits.PASSWORD_MINIMUM_LENGTH} caracteres.")
            .MaximumLength(ValidationLimits.PASSWORD_MAXIMUM_LENGTH)
            .WithMessage($"La contrasena no puede exceder {ValidationLimits.PASSWORD_MAXIMUM_LENGTH} caracteres.")
            .Matches(UPPERCASE_PATTERN).WithMessage("La contrasena debe incluir al menos una letra mayuscula.")
            .Matches(LOWERCASE_PATTERN).WithMessage("La contrasena debe incluir al menos una letra minuscula.")
            .Matches(DIGIT_PATTERN).WithMessage("La contrasena debe incluir al menos un numero.");
    }
}
