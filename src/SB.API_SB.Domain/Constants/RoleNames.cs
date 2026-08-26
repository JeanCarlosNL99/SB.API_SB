namespace SB.API_SB.Domain.Constants;

/// <summary>
/// Nombres de los roles del sistema. Se usan tanto para el sembrado inicial de
/// datos como para las politicas de autorizacion de la API.
/// </summary>
public static class RoleNames
{
    /// <summary>Acceso total, incluida la administracion de usuarios.</summary>
    public const string ADMINISTRATOR = "Administrador";

    /// <summary>Gestiona empleados, entidades gubernamentales y reportes de nomina.</summary>
    public const string HUMAN_RESOURCES = "RecursosHumanos";

    /// <summary>Solo lectura sobre los mantenimientos y reportes.</summary>
    public const string CONSULTANT = "Consultor";

    /// <summary>Listado de todos los roles disponibles.</summary>
    public static IReadOnlyCollection<string> All { get; } = new[]
    {
        ADMINISTRATOR,
        HUMAN_RESOURCES,
        CONSULTANT
    };
}
