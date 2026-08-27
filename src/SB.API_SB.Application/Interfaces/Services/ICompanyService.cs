using SB.API_SB.Application.Contracts.Companies;

namespace SB.API_SB.Application.Interfaces.Services;

/// <summary>Casos de uso del mantenimiento de companias.</summary>
public interface ICompanyService
{
    /// <summary>Obtiene todas las companias con su cantidad de empleados activos.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Companias registradas.</returns>
    Task<IReadOnlyCollection<CompanyResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene una compania por su identificador.</summary>
    /// <param name="companyId">Identificador de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La compania solicitada.</returns>
    Task<CompanyResponse> GetByIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>Registra una nueva compania.</summary>
    /// <param name="request">Datos de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La compania registrada.</returns>
    Task<CompanyResponse> CreateAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Actualiza una compania existente.</summary>
    /// <param name="companyId">Identificador de la compania.</param>
    /// <param name="request">Nuevos datos de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La compania actualizada.</returns>
    Task<CompanyResponse> UpdateAsync(
        Guid companyId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una compania que no tenga empleados ni nominas registradas.
    /// </summary>
    /// <param name="companyId">Identificador de la compania.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    Task DeleteAsync(Guid companyId, CancellationToken cancellationToken = default);
}
