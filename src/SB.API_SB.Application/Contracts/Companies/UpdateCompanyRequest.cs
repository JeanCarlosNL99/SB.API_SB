namespace SB.API_SB.Application.Contracts.Companies;

/// <summary>Datos modificables de una compania existente.</summary>
public sealed class UpdateCompanyRequest
{
    /// <summary>Razon social de la compania.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Registro Nacional de Contribuyente.</summary>
    public string TaxIdentificationNumber { get; set; } = string.Empty;

    /// <summary>Indica si la compania esta operando.</summary>
    public bool IsActive { get; set; }
}
