using SB.API_SB.Application.Interfaces.Common;

namespace SB.API_SB.Tests.TestDoubles;

/// <summary>
/// Reloj fijo para las pruebas. Al eliminar la dependencia de la hora real, los
/// resultados que incluyen fechas son deterministas y reproducibles.
/// </summary>
public sealed class FixedDateTimeProvider : IDateTimeProvider
{
    /// <summary>Fecha y hora utilizada por omision en las pruebas.</summary>
    public static readonly DateTime DEFAULT_DATE_TIME =
        new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

    public FixedDateTimeProvider()
        : this(DEFAULT_DATE_TIME)
    {
    }

    public FixedDateTimeProvider(DateTime fixedDateTime)
    {
        UtcNow = fixedDateTime;
    }

    /// <inheritdoc />
    public DateTime UtcNow { get; }
}
