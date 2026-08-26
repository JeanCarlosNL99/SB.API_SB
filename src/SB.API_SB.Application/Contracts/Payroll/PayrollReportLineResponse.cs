using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Contracts.Payroll;

/// <summary>Linea del reporte semanal de nomina correspondiente a un empleado.</summary>
public sealed class PayrollReportLineResponse
{
    /// <summary>Identificador del empleado.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Nombre completo del empleado.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Numero de seguro social del empleado.</summary>
    public string SocialSecurityNumber { get; set; } = string.Empty;

    /// <summary>Tipo de contrato del empleado.</summary>
    public EmployeeType Type { get; set; }

    /// <summary>Descripcion legible del tipo de contrato.</summary>
    public string TypeDescription { get; set; } = string.Empty;

    /// <summary>Departamento del empleado.</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>Estado laboral del empleado.</summary>
    public EmployeeStatus Status { get; set; }

    /// <summary>Monto a pagar en la semana.</summary>
    public decimal WeeklyPayment { get; set; }

    /// <summary>Desglose del calculo aplicado.</summary>
    public PaymentBreakdownResponse PaymentBreakdown { get; set; } = new();
}
