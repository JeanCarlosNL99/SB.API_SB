using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Criteria;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>Operaciones de persistencia especificas de empleados.</summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    /// <summary>
    /// Busca empleados aplicando los filtros en la base de datos y devuelve solo
    /// la pagina solicitada.
    /// </summary>
    /// <param name="criteria">Criterios de busqueda y paginacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Pagina de empleados que satisfacen el filtro.</returns>
    Task<PagedList<Employee>> SearchAsync(
        EmployeeFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene un empleado junto con su departamento.</summary>
    /// <param name="employeeId">Identificador del empleado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El empleado con su departamento o nulo si no existe.</returns>
    Task<Employee?> GetWithDepartmentAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene los empleados para el reporte semanal de nomina.</summary>
    /// <param name="onlyActiveEmployees">Indica si se limita a empleados activos.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Empleados con su departamento cargado.</returns>
    Task<IReadOnlyCollection<Employee>> GetForPayrollAsync(
        bool onlyActiveEmployees,
        CancellationToken cancellationToken = default);

    /// <summary>Determina si el numero de seguro social ya esta registrado.</summary>
    /// <param name="socialSecurityNumber">Numero de seguro social a verificar.</param>
    /// <param name="excludedEmployeeId">Empleado a excluir de la verificacion.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Verdadero si el numero ya existe.</returns>
    Task<bool> ExistsBySocialSecurityNumberAsync(
        string socialSecurityNumber,
        Guid? excludedEmployeeId = null,
        CancellationToken cancellationToken = default);
}
