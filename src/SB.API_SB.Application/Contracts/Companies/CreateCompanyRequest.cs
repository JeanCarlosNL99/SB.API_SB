namespace SB.API_SB.Application.Contracts.Companies;

/// <summary>Datos para registrar una nueva compania.</summary>
public sealed class CreateCompanyRequest
{
    /// <summary>Razon social de la compania.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Registro Nacional de Contribuyente.</summary>
    public string TaxIdentificationNumber { get; set; } = string.Empty;
}
