namespace SB.API_SB.Infrastructure.Options;

/// <summary>
/// Configuracion de la base de datos relacional. Se enlaza a la seccion
/// <c>Database</c> de AppSettings.json, de modo que ni la cadena de conexion ni
/// el proveedor quedan escritos en el codigo.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Nombre de la seccion de configuracion asociada.</summary>
    public const string SECTION_NAME = "Database";

    /// <summary>Proveedor relacional a utilizar: <c>SqlServer</c> o <c>Sqlite</c>.</summary>
    public string Provider { get; set; } = DatabaseProviderNames.SQL_SERVER;

    /// <summary>Indica si al iniciar se crea el esquema y se siembran los datos base.</summary>
    public bool ApplyAutomaticInitialization { get; set; } = true;

    /// <summary>Indica si se registran en el log las sentencias con sus parametros.</summary>
    public bool EnableSensitiveDataLogging { get; set; }
}

/// <summary>Nombres admitidos de proveedores de base de datos.</summary>
public static class DatabaseProviderNames
{
    /// <summary>Microsoft SQL Server.</summary>
    public const string SQL_SERVER = "SqlServer";

    /// <summary>SQLite, utilizado para ejecutar la solucion sin instalar un motor.</summary>
    public const string SQLITE = "Sqlite";
}
