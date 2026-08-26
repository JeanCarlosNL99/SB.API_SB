namespace SB.API_SB.Domain.Common;

/// <summary>
/// Clase base para toda entidad persistida. Centraliza el identificador y los
/// campos de auditoria, evitando repetirlos en cada entidad del dominio.
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>Identificador unico de la entidad.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Fecha y hora (UTC) en que se creo el registro.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Usuario que creo el registro.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Fecha y hora (UTC) de la ultima modificacion del registro.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Usuario que realizo la ultima modificacion del registro.</summary>
    public string? UpdatedBy { get; set; }
}
