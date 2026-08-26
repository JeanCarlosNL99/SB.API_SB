namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>
/// Definiciones de precision y longitud de columnas compartidas por varias
/// configuraciones. Evita repetir literales numericos en el mapeo.
/// </summary>
internal static class ColumnDefinitions
{
    /// <summary>Digitos totales de un importe monetario.</summary>
    public const int MONETARY_PRECISION = 18;

    /// <summary>Decimales de un importe monetario.</summary>
    public const int MONETARY_SCALE = 2;

    /// <summary>Digitos totales de una cantidad de horas.</summary>
    public const int HOURS_PRECISION = 8;

    /// <summary>Decimales de una cantidad de horas.</summary>
    public const int HOURS_SCALE = 2;

    /// <summary>Digitos totales de una tarifa de comision.</summary>
    public const int COMMISSION_RATE_PRECISION = 6;

    /// <summary>Decimales de una tarifa de comision.</summary>
    public const int COMMISSION_RATE_SCALE = 4;

    /// <summary>Longitud maxima de los campos de auditoria de usuario.</summary>
    public const int AUDIT_USER_MAXIMUM_LENGTH = 100;
}
