namespace SB.API_SB.Infrastructure.Options;

/// <summary>
/// Configuracion de la base de datos de texto plano que respalda el
/// mantenimiento de entidades gubernamentales.
/// </summary>
public sealed class FlatFileDatabaseOptions
{
    /// <summary>Nombre de la seccion de configuracion asociada.</summary>
    public const string SECTION_NAME = "FlatFileDatabase";

    /// <summary>
    /// Ruta del archivo de datos, relativa al directorio raiz del proyecto de la
    /// API. El archivo vive dentro del propio proyecto, tal como exige el
    /// requerimiento.
    /// </summary>
    public string GovernmentEntitiesFilePath { get; set; } =
        Path.Combine("Database", "GovernmentEntities.txt");

    /// <summary>Ruta del archivo semilla utilizado cuando el archivo de datos no existe.</summary>
    public string GovernmentEntitiesSeedFilePath { get; set; } =
        Path.Combine("Database", "GovernmentEntities.seed.txt");

    /// <summary>Directorio donde se guardan las copias de respaldo antes de reescribir.</summary>
    public string BackupDirectoryPath { get; set; } = Path.Combine("Database", "Backups");

    /// <summary>Indica si se genera una copia de respaldo antes de cada reescritura.</summary>
    public bool CreateBackupOnWrite { get; set; } = true;
}
