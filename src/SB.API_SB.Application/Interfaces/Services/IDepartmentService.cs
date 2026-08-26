using SB.API_SB.Application.Contracts.Departments;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>Casos de uso del mantenimiento de departamentos.</summary>
public interface IDepartmentService
{
    /// <summary>Obtiene todos los departamentos.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Departamentos registrados.</returns>
    Task<IReadOnlyCollection<DepartmentResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene un departamento por su identificador.</summary>
    /// <param name="departmentId">Identificador del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El departamento solicitado.</returns>
    Task<DepartmentResponse> GetByIdAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    /// <summary>Registra un nuevo departamento.</summary>
    /// <param name="request">Datos del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El departamento registrado.</returns>
    Task<DepartmentResponse> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Actualiza un departamento existente.</summary>
    /// <param name="departmentId">Identificador del departamento.</param>
    /// <param name="request">Nuevos datos del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El departamento actualizado.</returns>
    Task<DepartmentResponse> UpdateAsync(
        Guid departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Elimina un departamento sin empleados asignados.</summary>
    /// <param name="departmentId">Identificador del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task DeleteAsync(Guid departmentId, CancellationToken cancellationToken = default);
}
