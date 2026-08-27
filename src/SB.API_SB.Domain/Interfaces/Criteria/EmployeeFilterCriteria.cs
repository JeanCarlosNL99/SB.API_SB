using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Domain.Interfaces.Criteria;

/// <summary>
/// Criterios de busqueda de empleados. Se define en el dominio para que los
/// repositorios reciban un unico objeto en lugar de una lista creciente de
/// parametros, y para que el filtrado se resuelva en la base de datos.
/// </summary>
public sealed class EmployeeFilterCriteria
{
    /// <summary>Texto a buscar en el nombre o apellido del empleado.</summary>
    public string? Name { get; init; }

    /// <summary>Compania por la que se desea filtrar.</summary>
    public Guid? CompanyId { get; init; }

    /// <summary>Departamento por el que se desea filtrar.</summary>
    public Guid? DepartmentId { get; init; }

    /// <summary>Estado laboral por el que se desea filtrar.</summary>
    public EmployeeStatus? Status { get; init; }

    /// <summary>Tipo de empleado por el que se desea filtrar.</summary>
    public EmployeeType? Type { get; init; }

    /// <summary>Numero de pagina solicitado.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Cantidad de registros por pagina.</summary>
    public int PageSize { get; init; } = 10;
}
