using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Contracts.GovernmentEntities;

/// <summary>Entidad gubernamental expuesta por la API.</summary>
public sealed class GovernmentEntityResponse
{
    /// <summary>Identificador del registro.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre oficial de la entidad.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Categoria administrativa de la entidad.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Poder del Estado al que pertenece.</summary>
    public string StateBranch { get; set; } = string.Empty;

    /// <summary>Sector al que esta adscrita.</summary>
    public string Sector { get; set; } = string.Empty;

    /// <summary>Estado del registro.</summary>
    public RecordStatus Status { get; set; }

    /// <summary>Descripcion legible del estado del registro.</summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>Fecha y hora (UTC) de creacion.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Fecha y hora (UTC) de la ultima modificacion.</summary>
    public DateTime? UpdatedAt { get; set; }
}
