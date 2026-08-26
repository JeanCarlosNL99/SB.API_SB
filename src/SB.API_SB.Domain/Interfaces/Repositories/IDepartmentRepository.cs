using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>Operaciones de persistencia especificas de departamentos.</summary>
public interface IDepartmentRepository : IRepository<Department>
{
    /// <summary>Obtiene un departamento por su codigo.</summary>
    /// <param name="code">Codigo del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El departamento encontrado o nulo.</returns>
    Task<Department?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Indica si el departamento tiene empleados asignados.</summary>
    /// <param name="departmentId">Identificador del departamento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Verdadero si existen empleados asignados.</returns>
    Task<bool> HasEmployeesAsync(Guid departmentId, CancellationToken cancellationToken = default);
}
