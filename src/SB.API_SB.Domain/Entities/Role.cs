using SB.API_SB.Domain.Common;

namespace SB.API_SB.Domain.Entities;

/// <summary>Rol de seguridad. Agrupa los permisos que se emiten en el token JWT.</summary>
public sealed class Role : AuditableEntity
{
    /// <summary>Nombre unico del rol.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Descripcion funcional del rol.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Usuarios asociados al rol.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
