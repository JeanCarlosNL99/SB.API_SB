using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.Employees;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>Casos de uso del mantenimiento de empleados.</summary>
public interface IEmployeeService
{
    /// <summary>Consulta empleados con filtros por nombre, departamento y estado.</summary>
    /// <param name="filter">Filtros y paginacion solicitados.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de empleados con su pago semanal calculado.</returns>
    Task<PagedResponse<EmployeeResponse>> SearchAsync(
        EmployeeFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene un empleado por su identificador.</summary>
    /// <param name="employeeId">Identificador del empleado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El empleado solicitado.</returns>
    Task<EmployeeResponse> GetByIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>Registra un nuevo empleado.</summary>
    /// <param name="request">Datos del empleado a registrar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El empleado registrado.</returns>
    Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Actualiza un empleado y recalcula su pago semanal.</summary>
    /// <param name="employeeId">Identificador del empleado.</param>
    /// <param name="request">Nuevos datos del empleado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El empleado actualizado.</returns>
    Task<EmployeeResponse> UpdateAsync(
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Elimina un empleado.</summary>
    /// <param name="employeeId">Identificador del empleado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task DeleteAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
