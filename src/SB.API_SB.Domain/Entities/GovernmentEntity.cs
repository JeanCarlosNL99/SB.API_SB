using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Entidad gubernamental de la Republica Dominicana. Corresponde al
/// mantenimiento solicitado y se persiste en un archivo de texto plano ubicado
/// dentro del propio proyecto.
/// </summary>
public sealed class GovernmentEntity : AuditableEntity
{
    /// <summary>Nombre oficial de la entidad gubernamental.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Categoria administrativa (por ejemplo, Ministerio).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Poder del Estado al que pertenece la entidad.</summary>
    public string StateBranch { get; set; } = string.Empty;

    /// <summary>Sector al que esta adscrita la entidad.</summary>
    public string Sector { get; set; } = string.Empty;

    /// <summary>Estado del registro dentro del mantenimiento.</summary>
    public RecordStatus Status { get; set; } = RecordStatus.Active;
}
