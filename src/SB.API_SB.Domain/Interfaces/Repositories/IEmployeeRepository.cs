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

    /// <summary>
    /// Obtiene los empleados de una entidad gubernamental que deben incluirse en el calculo de
    /// la nomina semanal.
    /// </summary>
    /// <param name="governmentEntityId">Entidad gubernamental cuya nomina se calcula.</param>
    /// <param name="onlyActiveEmployees">Indica si se limita a empleados activos.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Empleados con su departamento cargado.</returns>
    Task<IReadOnlyCollection<Employee>> GetForPayrollAsync(
        Guid governmentEntityId,
        bool onlyActiveEmployees,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuenta los empleados agrupados por entidad gubernamental.
    /// </summary>
    /// <remarks>
    /// El agrupamiento se resuelve en el motor de base de datos y devuelve una
    /// fila por entidad, no la tabla de empleados. Es lo que permite listar las
    /// entidades con nomina sin traer los empleados a memoria.
    /// </remarks>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Conteo de empleados por entidad gubernamental.</returns>
    Task<IReadOnlyCollection<GovernmentEntityEmployeeCount>> CountByGovernmentEntityAsync(
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

/// <summary>Cantidad de empleados registrados en una entidad gubernamental.</summary>
/// <param name="GovernmentEntityId">Entidad gubernamental contada.</param>
/// <param name="TotalEmployeeCount">Empleados registrados, en cualquier estado.</param>
/// <param name="ActiveEmployeeCount">Empleados activos, que son los que generan pago.</param>
public sealed record GovernmentEntityEmployeeCount(
    Guid GovernmentEntityId,
    int TotalEmployeeCount,
    int ActiveEmployeeCount);
