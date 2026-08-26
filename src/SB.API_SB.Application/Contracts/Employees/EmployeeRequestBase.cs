using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Contracts.Employees;

/// <summary>
/// Datos comunes al alta y a la actualizacion de un empleado.
/// </summary>
/// <remarks>
/// Los campos especificos de cada tipo son opcionales en el contrato y se
/// vuelven obligatorios mediante validaciones condicionales segun
/// <see cref="Type"/>. Este enfoque mantiene un unico contrato para el cliente
/// y concentra las reglas por tipo en el validador y en el manejador de tipo
/// correspondiente.
/// </remarks>
public abstract class EmployeeRequestBase
{
    /// <summary>Tipo de contrato del empleado.</summary>
    public EmployeeType Type { get; set; }

    /// <summary>Primer nombre. Obligatorio salvo para el empleado por horas.</summary>
    public string? FirstName { get; set; }

    /// <summary>Apellido paterno. Obligatorio para todos los tipos.</summary>
    public string PaternalLastName { get; set; } = string.Empty;

    /// <summary>Numero de seguro social.</summary>
    public string SocialSecurityNumber { get; set; } = string.Empty;

    /// <summary>Departamento al que se asigna el empleado.</summary>
    public Guid DepartmentId { get; set; }

    /// <summary>Estado laboral del empleado.</summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>Salario semanal. Aplica al empleado asalariado.</summary>
    public decimal? WeeklySalary { get; set; }

    /// <summary>Sueldo por hora. Aplica al empleado por horas.</summary>
    public decimal? HourlyWage { get; set; }

    /// <summary>Horas trabajadas en la semana. Aplica al empleado por horas.</summary>
    public decimal? HoursWorked { get; set; }

    /// <summary>Ventas brutas de la semana. Aplica a los empleados por comision.</summary>
    public decimal? GrossSales { get; set; }

    /// <summary>Tarifa de comision como fraccion decimal. Aplica a los empleados por comision.</summary>
    public decimal? CommissionRate { get; set; }

    /// <summary>Salario base. Aplica al empleado asalariado por comision.</summary>
    public decimal? BaseSalary { get; set; }
}
