using SB.API_SB.Application.Common;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Contracts.Employees;

/// <summary>
/// Filtros aceptados por la consulta de empleados: nombre, departamento y
/// estado, tal como exigen los requisitos funcionales.
/// </summary>
public sealed class EmployeeFilterRequest : PaginationRequest
{
    /// <summary>Texto a buscar en el nombre o apellido del empleado.</summary>
    public string? Name { get; set; }

    /// <summary>Compania por la que se filtra.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Departamento por el que se filtra.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Estado laboral por el que se filtra.</summary>
    public EmployeeStatus? Status { get; set; }

    /// <summary>Tipo de empleado por el que se filtra.</summary>
    public EmployeeType? Type { get; set; }
}
