using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Interfaces.Employees;

/// <summary>
/// Localiza el manejador correspondiente a un tipo de empleado. Sustituye a una
/// cadena de condicionales por una busqueda en un registro poblado por
/// inyeccion de dependencias.
/// </summary>
public interface IEmployeeTypeHandlerResolver
{
    /// <summary>Obtiene el manejador del tipo indicado.</summary>
    /// <param name="employeeType">Tipo de empleado solicitado.</param>
    /// <returns>Manejador registrado para el tipo.</returns>
    IEmployeeTypeHandler Resolve(EmployeeType employeeType);
}
