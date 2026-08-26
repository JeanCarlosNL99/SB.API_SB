namespace SB.API_SB.Application.Interfaces.Common;

/// <summary>
/// Abstraccion del reloj del sistema. Permite que los servicios sean
/// deterministas y por tanto verificables en pruebas unitarias, sin depender de
/// la hora real de la maquina.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Fecha y hora actual expresada en UTC.</summary>
    DateTime UtcNow { get; }
}
