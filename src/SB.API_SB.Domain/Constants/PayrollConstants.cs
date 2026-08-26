namespace SB.API_SB.Domain.Constants;

/// <summary>
/// Constantes del calculo de nomina. Se declaran aqui para eliminar numeros
/// magicos del codigo y concentrar en un unico lugar los parametros que la
/// normativa laboral puede modificar.
/// </summary>
public static class PayrollConstants
{
    /// <summary>Cantidad de horas de una jornada semanal ordinaria.</summary>
    public const decimal STANDARD_WEEKLY_HOURS = 40m;

    /// <summary>Factor de recargo aplicado a las horas trabajadas por encima de la jornada ordinaria.</summary>
    public const decimal OVERTIME_RATE_MULTIPLIER = 1.5m;

    /// <summary>Porcentaje adicional que recibe el empleado asalariado por comision sobre su salario base.</summary>
    public const decimal BASE_SALARY_BONUS_PERCENTAGE = 0.10m;

    /// <summary>Cantidad de decimales a la que se redondea todo importe monetario.</summary>
    public const int CURRENCY_DECIMAL_PLACES = 2;

    /// <summary>Cantidad maxima de horas que se pueden registrar en una semana.</summary>
    public const decimal MAXIMUM_WEEKLY_HOURS = 168m;

    /// <summary>Valor maximo permitido para una tarifa de comision (100%).</summary>
    public const decimal MAXIMUM_COMMISSION_RATE = 1m;
}
