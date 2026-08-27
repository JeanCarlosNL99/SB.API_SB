using System.Globalization;
using SB.API_SB.Domain.Exceptions;

namespace SB.API_SB.Domain.ValueObjects;

/// <summary>
/// Semana de nomina, identificada por ano y numero de semana segun la norma
/// ISO 8601.
/// </summary>
/// <remarks>
/// Usar el par (ano, numero de semana) en lugar de un rango de fechas es lo que
/// hace posible garantizar que una semana se pague una sola vez: dos peticiones
/// que describan la misma semana producen exactamente la misma clave, sin
/// importar con que fecha del periodo se hayan construido. La norma ISO 8601
/// tambien resuelve el caso de las semanas que cruzan el fin de ano, donde el ano
/// de la semana puede no coincidir con el ano del calendario.
/// </remarks>
public sealed record PayrollWeek
{
    /// <summary>Primer numero de semana valido.</summary>
    public const int FIRST_WEEK_NUMBER = 1;

    /// <summary>Ultimo numero de semana posible en un ano ISO 8601.</summary>
    public const int LAST_POSSIBLE_WEEK_NUMBER = 53;

    /// <summary>Primer ano admitido, para descartar valores evidentemente erroneos.</summary>
    public const int MINIMUM_YEAR = 2000;

    /// <summary>Ultimo ano admitido.</summary>
    public const int MAXIMUM_YEAR = 2100;

    private PayrollWeek(int year, int weekNumber)
    {
        Year = year;
        WeekNumber = weekNumber;
    }

    /// <summary>Ano ISO 8601 al que pertenece la semana.</summary>
    public int Year { get; }

    /// <summary>Numero de semana dentro del ano.</summary>
    public int WeekNumber { get; }

    /// <summary>Primer dia de la semana (lunes).</summary>
    public DateOnly StartDate => DateOnly.FromDateTime(
        ISOWeek.ToDateTime(Year, WeekNumber, DayOfWeek.Monday));

    /// <summary>Ultimo dia de la semana (domingo).</summary>
    public DateOnly EndDate => StartDate.AddDays(6);

    /// <summary>Etiqueta legible de la semana, por ejemplo <c>2026-S35</c>.</summary>
    public string Label => string.Create(
        CultureInfo.InvariantCulture,
        $"{Year:D4}-S{WeekNumber:D2}");

    /// <summary>
    /// Construye una semana de nomina validando que el ano y el numero de semana
    /// existan realmente en el calendario ISO 8601.
    /// </summary>
    /// <param name="year">Ano de la semana.</param>
    /// <param name="weekNumber">Numero de semana.</param>
    /// <returns>La semana solicitada.</returns>
    /// <exception cref="BusinessRuleViolationException">
    /// Si el ano esta fuera del rango admitido o el numero de semana no existe en
    /// ese ano.
    /// </exception>
    public static PayrollWeek Create(int year, int weekNumber)
    {
        if (year < MINIMUM_YEAR || year > MAXIMUM_YEAR)
        {
            throw new BusinessRuleViolationException(
                $"El ano de la semana de nomina debe estar entre {MINIMUM_YEAR} y {MAXIMUM_YEAR}.");
        }

        if (weekNumber < FIRST_WEEK_NUMBER || weekNumber > LAST_POSSIBLE_WEEK_NUMBER)
        {
            throw new BusinessRuleViolationException(
                $"El numero de semana debe estar entre {FIRST_WEEK_NUMBER} y " +
                $"{LAST_POSSIBLE_WEEK_NUMBER}.");
        }

        // No todos los anos tienen 53 semanas: la comprobacion evita aceptar una
        // semana que no existe en el calendario.
        int weeksInYear = ISOWeek.GetWeeksInYear(year);

        if (weekNumber > weeksInYear)
        {
            throw new BusinessRuleViolationException(
                $"El ano {year} tiene {weeksInYear} semanas, por lo que la semana " +
                $"{weekNumber} no existe.");
        }

        return new PayrollWeek(year, weekNumber);
    }

    /// <summary>Obtiene la semana de nomina que contiene la fecha indicada.</summary>
    /// <param name="date">Fecha dentro de la semana buscada.</param>
    /// <returns>La semana que contiene esa fecha.</returns>
    public static PayrollWeek FromDate(DateOnly date)
    {
        DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);

        return new PayrollWeek(ISOWeek.GetYear(dateTime), ISOWeek.GetWeekOfYear(dateTime));
    }

    /// <summary>Obtiene la semana de nomina en curso.</summary>
    /// <param name="currentDateTimeUtc">Fecha y hora actual en UTC.</param>
    /// <returns>La semana en curso.</returns>
    public static PayrollWeek Current(DateTime currentDateTimeUtc) =>
        FromDate(DateOnly.FromDateTime(currentDateTimeUtc));

    /// <summary>Devuelve la semana anterior a la actual.</summary>
    /// <returns>La semana inmediatamente anterior.</returns>
    public PayrollWeek Previous() => FromDate(StartDate.AddDays(-7));

    /// <summary>Devuelve la semana siguiente a la actual.</summary>
    /// <returns>La semana inmediatamente posterior.</returns>
    public PayrollWeek Next() => FromDate(StartDate.AddDays(7));

    /// <inheritdoc />
    public override string ToString() => Label;
}
