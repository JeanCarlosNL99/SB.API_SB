namespace SB.API_SB.Infrastructure.Options;

/// <summary>
/// Datos del usuario administrador inicial y de los datos de demostracion. Se
/// leen de AppSettings.json para no dejar credenciales escritas en el codigo.
/// </summary>
public sealed class SeedOptions
{
    /// <summary>Nombre de la seccion de configuracion asociada.</summary>
    public const string SECTION_NAME = "Seed";

    /// <summary>Nombre de usuario del administrador inicial.</summary>
    public string AdministratorUserName { get; set; } = "administrador";

    /// <summary>Correo electronico del administrador inicial.</summary>
    public string AdministratorEmail { get; set; } = "administrador@sb.gob.do";

    /// <summary>Nombre completo del administrador inicial.</summary>
    public string AdministratorFullName { get; set; } = "Administrador del Sistema";

    /// <summary>
    /// Contrasena inicial del administrador. Debe cambiarse en el primer inicio de
    /// sesion y sustituirse por un secreto de entorno en produccion.
    /// </summary>
    public string AdministratorPassword { get; set; } = string.Empty;

    /// <summary>
    /// Indica si se crean empleados y departamentos de demostracion para poder
    /// probar los cuatro tipos de calculo de nomina sin capturarlos a mano.
    /// </summary>
    public bool CreateDemonstrationData { get; set; } = true;
}
