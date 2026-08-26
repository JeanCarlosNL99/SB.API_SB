namespace SB.API_SB.Application.Contracts.Departments;

/// <summary>Departamento expuesto por la API.</summary>
public sealed class DepartmentResponse
{
    /// <summary>Identificador del departamento.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre del departamento.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Codigo corto del departamento.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Indica si el departamento esta vigente.</summary>
    public bool IsActive { get; set; }

    /// <summary>Cantidad de empleados asignados al departamento.</summary>
    public int EmployeeCount { get; set; }
}
