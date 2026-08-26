using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Application.Mappings;

/// <summary>Proyecciones de la jerarquia de empleados hacia sus contratos publicos.</summary>
public static class EmployeeMappings
{
    /// <summary>
    /// Convierte un empleado en su respuesta de API. Los campos comunes se copian
    /// aqui y los especificos de cada tipo los aporta el manejador del tipo,
    /// evitando condicionales por subclase en esta capa.
    /// </summary>
    /// <param name="employee">Empleado de dominio.</param>
    /// <param name="typeHandler">Manejador del tipo de empleado.</param>
    /// <param name="includePaymentBreakdown">Indica si se incluye el desglose del calculo.</param>
    /// <returns>Respuesta lista para devolverse desde la API.</returns>
    public static EmployeeResponse ToResponse(
        this Employee employee,
        IEmployeeTypeHandler typeHandler,
        bool includePaymentBreakdown = true)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(typeHandler);

        EmployeeResponse response = new()
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            PaternalLastName = employee.PaternalLastName,
            FullName = employee.FullName,
            SocialSecurityNumber = employee.SocialSecurityNumber,
            Type = employee.Type,
            TypeDescription = typeHandler.TypeDescription,
            Status = employee.Status,
            StatusDescription = employee.Status.Describe(),
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            WeeklyPayment = employee.CalculateWeeklyPayment(),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };

        typeHandler.ProjectTypeSpecificValues(employee, response);

        if (includePaymentBreakdown)
        {
            response.PaymentBreakdown = employee.BuildPaymentBreakdown().ToResponse();
        }

        return response;
    }

    /// <summary>Copia en la entidad los datos comunes a todos los tipos de empleado.</summary>
    /// <param name="employee">Entidad destino.</param>
    /// <param name="request">Datos de origen.</param>
    public static void ApplyCommonValues(this Employee employee, EmployeeRequestBase request)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(request);

        employee.FirstName = request.FirstName?.Trim();
        employee.PaternalLastName = request.PaternalLastName.Trim();
        employee.SocialSecurityNumber = request.SocialSecurityNumber.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.Status = request.Status;
    }
}
