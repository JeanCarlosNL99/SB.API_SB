using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Domain.Interfaces.Repositories;

/// <summary>Operaciones de persistencia especificas de companias.</summary>
public interface ICompanyRepository : IRepository<Company>
{
    /// <summary>Obtiene una compania por su Registro Nacional de Contribuyente.</summary>
    /// <param name="taxIdentificationNumber">Numero de registro a buscar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La compania encontrada o nulo.</returns>
    Task<Company?> GetByTaxIdentificationNumberAsync(
        string taxIdentificationNumber,
        CancellationToken cancellationToken = default);

    /// <summary>Indica si la compania tiene empleados registrados.</summary>
    /// <param name="companyId">Identificador de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Verdadero si existen empleados asociados.</returns>
    Task<bool> HasEmployeesAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Indica si la compania tiene ejecuciones de nomina registradas.</summary>
    /// <param name="companyId">Identificador de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Verdadero si existen ejecuciones de nomina asociadas.</returns>
    Task<bool> HasPayrollRunsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el resumen de empleados activos por compania.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Cantidad de empleados activos indexada por identificador de compania.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetActiveEmployeeCountsAsync(
        CancellationToken cancellationToken = default);
}
