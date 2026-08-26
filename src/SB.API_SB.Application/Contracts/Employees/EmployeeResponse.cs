using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Contracts.Employees;

/// <summary>
/// Empleado expuesto por la API, con su pago semanal ya calculado por el
/// dominio para que el cliente no tenga que replicar las formulas.
/// </summary>
public sealed class EmployeeResponse
{
    /// <summary>Identificador del empleado.</summary>
    public Guid Id { get; set; }

    /// <summary>Primer nombre del empleado.</summary>
    public string? FirstName { get; set; }

    /// <summary>Apellido paterno del empleado.</summary>
    public string PaternalLastName { get; set; } = string.Empty;

    /// <summary>Nombre completo listo para mostrarse.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Numero de seguro social.</summary>
    public string SocialSecurityNumber { get; set; } = string.Empty;

    /// <summary>Tipo de contrato del empleado.</summary>
    public EmployeeType Type { get; set; }

    /// <summary>Descripcion legible del tipo de contrato.</summary>
    public string TypeDescription { get; set; } = string.Empty;

    /// <summary>Estado laboral del empleado.</summary>
    public EmployeeStatus Status { get; set; }

    /// <summary>Descripcion legible del estado laboral.</summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>Identificador del departamento asignado.</summary>
    public Guid DepartmentId { get; set; }

    /// <summary>Nombre del departamento asignado.</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>Salario semanal, cuando aplica al tipo de empleado.</summary>
    public decimal? WeeklySalary { get; set; }

    /// <summary>Sueldo por hora, cuando aplica al tipo de empleado.</summary>
    public decimal? HourlyWage { get; set; }

    /// <summary>Horas trabajadas, cuando aplica al tipo de empleado.</summary>
    public decimal? HoursWorked { get; set; }

    /// <summary>Ventas brutas, cuando aplica al tipo de empleado.</summary>
    public decimal? GrossSales { get; set; }

    /// <summary>Tarifa de comision, cuando aplica al tipo de empleado.</summary>
    public decimal? CommissionRate { get; set; }

    /// <summary>Salario base, cuando aplica al tipo de empleado.</summary>
    public decimal? BaseSalary { get; set; }

    /// <summary>Pago semanal calculado por el dominio.</summary>
    public decimal WeeklyPayment { get; set; }

    /// <summary>Desglose del calculo del pago semanal.</summary>
    public PaymentBreakdownResponse? PaymentBreakdown { get; set; }

    /// <summary>Fecha y hora (UTC) de creacion del registro.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Fecha y hora (UTC) de la ultima modificacion.</summary>
    public DateTime? UpdatedAt { get; set; }
}
