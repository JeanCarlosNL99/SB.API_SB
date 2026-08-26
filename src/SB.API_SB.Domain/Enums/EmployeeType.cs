namespace SB.API_SB.Domain.Enums;

/// <summary>
/// Tipos de empleado soportados por el calculo de nomina. El valor entero es
/// estable porque se persiste como discriminador en la base de datos.
/// </summary>
public enum EmployeeType
{
    /// <summary>Empleado asalariado: cobra un salario semanal fijo.</summary>
    Salaried = 1,

    /// <summary>Empleado por horas: cobra por hora trabajada con recargo por horas extras.</summary>
    Hourly = 2,

    /// <summary>Empleado por comision: cobra un porcentaje de sus ventas brutas.</summary>
    Commission = 3,

    /// <summary>Empleado asalariado por comision: salario base mas comision.</summary>
    BaseSalariedCommission = 4
}
