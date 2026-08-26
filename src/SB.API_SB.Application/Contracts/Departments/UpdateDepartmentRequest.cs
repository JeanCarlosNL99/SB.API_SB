namespace SB.API_SB.Application.Contracts.Departments;

/// <summary>Datos modificables de un departamento existente.</summary>
public sealed class UpdateDepartmentRequest
{
    /// <summary>Nombre del departamento.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Codigo corto e irrepetible del departamento.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Indica si el departamento esta vigente.</summary>
    public bool IsActive { get; set; }
}
