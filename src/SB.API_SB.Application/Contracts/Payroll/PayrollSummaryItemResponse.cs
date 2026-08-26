namespace SB.API_SB.Application.Contracts.Payroll;

/// <summary>Total agregado de nomina para un agrupamiento determinado.</summary>
public sealed class PayrollSummaryItemResponse
{
    /// <summary>Nombre del grupo (tipo de contrato o departamento).</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>Cantidad de empleados en el grupo.</summary>
    public int EmployeeCount { get; set; }

    /// <summary>Monto total a pagar en el grupo.</summary>
    public decimal TotalWeeklyPayment { get; set; }
}
