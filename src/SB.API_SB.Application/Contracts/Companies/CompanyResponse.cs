namespace SB.API_SB.Application.Contracts.Companies;

/// <summary>Compania expuesta por la API.</summary>
public sealed class CompanyResponse
{
    /// <summary>Identificador de la compania.</summary>
    public Guid Id { get; set; }

    /// <summary>Razon social.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Registro Nacional de Contribuyente.</summary>
    public string TaxIdentificationNumber { get; set; } = string.Empty;

    /// <summary>Indica si la compania esta operando.</summary>
    public bool IsActive { get; set; }

    /// <summary>Cantidad de empleados activos, que son los que entran a la nomina.</summary>
    public int ActiveEmployeeCount { get; set; }

    /// <summary>Fecha y hora (UTC) de creacion del registro.</summary>
    public DateTime CreatedAt { get; set; }
}
