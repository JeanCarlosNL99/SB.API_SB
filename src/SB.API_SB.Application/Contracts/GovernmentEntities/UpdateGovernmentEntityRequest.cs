using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Contracts.GovernmentEntities;

/// <summary>Datos modificables de una entidad gubernamental existente.</summary>
public sealed class UpdateGovernmentEntityRequest
{
    /// <summary>Nombre oficial de la entidad.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Categoria administrativa de la entidad.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Poder del Estado al que pertenece.</summary>
    public string StateBranch { get; set; } = string.Empty;

    /// <summary>Sector al que esta adscrita.</summary>
    public string Sector { get; set; } = string.Empty;

    /// <summary>Estado del registro.</summary>
    public RecordStatus Status { get; set; } = RecordStatus.Active;
}
