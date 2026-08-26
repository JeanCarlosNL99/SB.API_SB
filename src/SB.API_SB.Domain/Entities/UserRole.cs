namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Entidad de union entre usuarios y roles. Se modela explicitamente para poder
/// auditar cuando y quien realizo la asignacion.
/// </summary>
public sealed class UserRole
{
    /// <summary>Identificador del usuario.</summary>
    public Guid UserId { get; set; }

    /// <summary>Usuario asociado.</summary>
    public User? User { get; set; }

    /// <summary>Identificador del rol.</summary>
    public Guid RoleId { get; set; }

    /// <summary>Rol asociado.</summary>
    public Role? Role { get; set; }

    /// <summary>Fecha y hora (UTC) en que se asigno el rol.</summary>
    public DateTime AssignedAt { get; set; }
}
