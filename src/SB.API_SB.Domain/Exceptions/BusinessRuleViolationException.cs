namespace SB.API_SB.Domain.Exceptions;

/// <summary>Se lanza cuando una operacion incumple una regla de negocio.</summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public override string ErrorCode => "REGLA_DE_NEGOCIO_INCUMPLIDA";
}
