namespace SB.API_SB.Application.Contracts.Users;

/// <summary>Rol disponible en el sistema.</summary>
public sealed class RoleResponse
{
    /// <summary>Identificador del rol.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre del rol.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Descripcion funcional del rol.</summary>
    public string Description { get; set; } = string.Empty;
}
