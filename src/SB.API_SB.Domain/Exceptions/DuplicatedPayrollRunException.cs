namespace SB.API_SB.Domain.Exceptions;

/// <summary>
/// Se lanza al intentar generar la nomina de una semana que la compania ya pago.
/// </summary>
/// <remarks>
/// Es una excepcion propia y no un duplicado generico porque el cliente necesita
/// saber cual es la ejecucion existente para poder consultarla, y porque el
/// mensaje debe explicar la regla, no solo el conflicto.
/// </remarks>
public sealed class DuplicatedPayrollRunException : DomainException
{
    public DuplicatedPayrollRunException(
        string companyName,
        int year,
        int weekNumber,
        Guid existingPayrollRunId)
        : base($"La compania '{companyName}' ya tiene la nomina de la semana {weekNumber} " +
               $"del ano {year} generada. Una semana solo puede pagarse una vez; anule la " +
               $"ejecucion existente si necesita volver a calcularla.")
    {
        CompanyName = companyName;
        Year = year;
        WeekNumber = weekNumber;
        ExistingPayrollRunId = existingPayrollRunId;
    }

    /// <summary>Compania que ya tiene la semana pagada.</summary>
    public string CompanyName { get; }

    /// <summary>Ano de la semana en conflicto.</summary>
    public int Year { get; }

    /// <summary>Numero de la semana en conflicto.</summary>
    public int WeekNumber { get; }

    /// <summary>Identificador de la ejecucion que ya existe.</summary>
    public Guid ExistingPayrollRunId { get; }

    /// <inheritdoc />
    public override string ErrorCode => "NOMINA_SEMANA_YA_GENERADA";
}
