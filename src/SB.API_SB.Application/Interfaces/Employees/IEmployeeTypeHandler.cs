using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Interfaces.Employees;

/// <summary>
/// Manejador de un tipo concreto de empleado. Encapsula como se construye la
/// entidad, como se aplican los cambios y como se proyectan sus campos
/// especificos hacia la respuesta de la API.
/// </summary>
/// <remarks>
/// Es la pieza que hace escalable el modulo de empleados: para soportar un nuevo
/// tipo de contrato basta con crear una subclase de <see cref="Employee"/> y un
/// nuevo manejador, registrarlo en el contenedor de dependencias y el resto del
/// sistema sigue funcionando sin cambios (Principio Abierto/Cerrado).
/// </remarks>
public interface IEmployeeTypeHandler
{
    /// <summary>Tipo de empleado que atiende este manejador.</summary>
    EmployeeType HandledType { get; }

    /// <summary>Descripcion legible del tipo de empleado.</summary>
    string TypeDescription { get; }

    /// <summary>Crea una nueva entidad de empleado a partir de la solicitud.</summary>
    /// <param name="request">Datos capturados para el empleado.</param>
    /// <returns>Entidad de dominio del subtipo correspondiente.</returns>
    Employee CreateEmployee(EmployeeRequestBase request);

    /// <summary>
    /// Aplica sobre una entidad existente los datos especificos del tipo, de modo
    /// que el pago semanal se recalcule con los nuevos valores.
    /// </summary>
    /// <param name="employee">Entidad a actualizar.</param>
    /// <param name="request">Datos con los nuevos valores.</param>
    void ApplyTypeSpecificValues(Employee employee, EmployeeRequestBase request);

    /// <summary>Copia hacia la respuesta los campos propios del tipo de empleado.</summary>
    /// <param name="employee">Entidad de origen.</param>
    /// <param name="response">Respuesta a completar.</param>
    void ProjectTypeSpecificValues(Employee employee, EmployeeResponse response);
}
