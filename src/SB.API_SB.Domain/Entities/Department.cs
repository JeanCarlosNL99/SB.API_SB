using SB.API_SB.Domain.Common;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Departamento organizacional al que se asignan los empleados. Permite el
/// filtro por departamento exigido en los requisitos funcionales.
/// </summary>
public sealed class Department : AuditableEntity
{
    /// <summary>Nombre del departamento.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Codigo corto e irrepetible del departamento.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Indica si el departamento esta vigente.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Empleados asignados al departamento.</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
