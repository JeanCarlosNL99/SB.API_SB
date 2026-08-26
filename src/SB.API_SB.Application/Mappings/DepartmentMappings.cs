using SB.API_SB.Application.Contracts.Departments;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Application.Mappings;

/// <summary>Proyecciones del mantenimiento de departamentos.</summary>
public static class DepartmentMappings
{
    /// <summary>Convierte un departamento en su respuesta de API.</summary>
    /// <param name="department">Departamento de dominio.</param>
    /// <returns>Respuesta lista para devolverse desde la API.</returns>
    public static DepartmentResponse ToResponse(this Department department)
    {
        ArgumentNullException.ThrowIfNull(department);

        return new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            IsActive = department.IsActive,
            EmployeeCount = department.Employees.Count
        };
    }
}
