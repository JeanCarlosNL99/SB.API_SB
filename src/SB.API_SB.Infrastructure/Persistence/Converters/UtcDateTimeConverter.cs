using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SB.API_SB.Infrastructure.Persistence.Converters;

/// <summary>
/// Conversor que garantiza que toda fecha viaje a la base de datos en UTC y
/// regrese marcada como UTC.
/// </summary>
/// <remarks>
/// Los proveedores relacionales devuelven las fechas con
/// <see cref="DateTimeKind.Unspecified"/>, y al serializarlas a JSON quedan sin el
/// sufijo que indica la zona. El cliente las interpreta entonces como hora local
/// y muestra un desfase igual al de su zona horaria. Aplicar este conversor a
/// todas las propiedades de fecha resuelve el problema en el modelo, una sola vez,
/// en lugar de corregirlo en cada consulta o en cada pantalla.
/// </remarks>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            dateTime => dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : dateTime.ToUniversalTime(),
            dateTime => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
    {
    }
}

/// <summary>
/// Version del conversor para propiedades de fecha opcionales.
/// </summary>
public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            dateTime => dateTime.HasValue
                ? dateTime.Value.Kind == DateTimeKind.Utc
                    ? dateTime
                    : dateTime.Value.ToUniversalTime()
                : dateTime,
            dateTime => dateTime.HasValue
                ? DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc)
                : dateTime)
    {
    }
}
