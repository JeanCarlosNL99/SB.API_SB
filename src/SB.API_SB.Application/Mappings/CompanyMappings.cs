using SB.API_SB.Application.Contracts.Companies;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Application.Mappings;

/// <summary>Proyecciones del mantenimiento de companias.</summary>
public static class CompanyMappings
{
    /// <summary>Convierte una compania en su respuesta de API.</summary>
    /// <param name="company">Compania de dominio.</param>
    /// <param name="activeEmployeeCount">Cantidad de empleados activos de la compania.</param>
    /// <returns>Respuesta lista para devolverse desde la API.</returns>
    public static CompanyResponse ToResponse(this Company company, int activeEmployeeCount)
    {
        ArgumentNullException.ThrowIfNull(company);

        return new CompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            TaxIdentificationNumber = company.TaxIdentificationNumber,
            IsActive = company.IsActive,
            ActiveEmployeeCount = activeEmployeeCount,
            CreatedAt = company.CreatedAt
        };
    }
}
