namespace SB.API_SB.Domain.Enums;

/// <summary>Estado laboral de un empleado. Se usa como filtro en las consultas.</summary>
public enum EmployeeStatus
{
    /// <summary>Empleado activo: se incluye en el reporte semanal de nomina.</summary>
    Active = 1,

    /// <summary>Empleado inactivo: se conserva el historico pero no se le calcula pago.</summary>
    Inactive = 2
}
