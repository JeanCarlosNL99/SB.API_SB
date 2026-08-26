using SB.API_SB.Application.Interfaces.Common;

namespace SB.API_SB.Infrastructure.Common;

/// <summary>
/// Implementacion del proveedor de fecha y hora basada en el reloj del sistema.
/// Es la unica clase de la solucion que lee la hora real, lo que permite
/// sustituirla por un doble de prueba en los tests.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
