using SB.API_SB.Domain.Common;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Compania para la que se calculan los pagos semanales.
/// </summary>
/// <remarks>
/// Es la unidad sobre la que se ejecuta la nomina: cada empleado pertenece a una
/// compania y cada ejecucion de nomina corresponde a una compania y una semana.
/// </remarks>
public sealed class Company : AuditableEntity
{
    /// <summary>Razon social de la compania.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Registro Nacional de Contribuyente de la compania.</summary>
    public string TaxIdentificationNumber { get; set; } = string.Empty;

    /// <summary>Indica si la compania esta operando.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Empleados que pertenecen a la compania.</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <summary>Ejecuciones de nomina realizadas para la compania.</summary>
    public ICollection<PayrollRun> PayrollRuns { get; set; } = new List<PayrollRun>();
}
