namespace SB.API_SB.Application.Contracts.Departments;

/// <summary>Datos para registrar un nuevo departamento.</summary>
public sealed class CreateDepartmentRequest
{
    /// <summary>Nombre del departamento.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Codigo corto e irrepetible del departamento.</summary>
    public string Code { get; set; } = string.Empty;
}
