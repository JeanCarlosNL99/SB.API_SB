namespace SB.API_SB.Presentation.Authorization;

/// <summary>
/// Nombres de las politicas de autorizacion de la API. Usar constantes en lugar
/// de literales evita que un error de escritura deje un endpoint sin proteger.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Permite solo al rol administrador.</summary>
    public const string ADMINISTRATION_ONLY = "SoloAdministracion";

    /// <summary>Permite a los roles que pueden modificar los mantenimientos.</summary>
    public const string MAINTENANCE_WRITE = "EscrituraMantenimiento";

    /// <summary>Permite a cualquier usuario autenticado consultar informacion.</summary>
    public const string MAINTENANCE_READ = "LecturaMantenimiento";
}
