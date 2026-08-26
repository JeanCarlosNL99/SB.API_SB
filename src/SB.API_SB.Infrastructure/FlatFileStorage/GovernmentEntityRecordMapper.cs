using System.Globalization;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Infrastructure.FlatFileStorage;

/// <summary>
/// Traduce entre una entidad gubernamental y su representacion como linea de
/// texto delimitada.
/// </summary>
public static class GovernmentEntityRecordMapper
{
    /// <summary>Cantidad de campos que compone un registro completo.</summary>
    public const int FULL_RECORD_FIELD_COUNT = 10;

    /// <summary>Cantidad de campos de un registro semilla (solo datos de negocio).</summary>
    public const int SEED_RECORD_FIELD_COUNT = 4;

    private const int FIELD_INDEX_IDENTIFIER = 0;
    private const int FIELD_INDEX_NAME = 1;
    private const int FIELD_INDEX_CATEGORY = 2;
    private const int FIELD_INDEX_STATE_BRANCH = 3;
    private const int FIELD_INDEX_SECTOR = 4;
    private const int FIELD_INDEX_STATUS = 5;
    private const int FIELD_INDEX_CREATED_AT = 6;
    private const int FIELD_INDEX_CREATED_BY = 7;
    private const int FIELD_INDEX_UPDATED_AT = 8;
    private const int FIELD_INDEX_UPDATED_BY = 9;

    private const int SEED_FIELD_INDEX_NAME = 0;
    private const int SEED_FIELD_INDEX_CATEGORY = 1;
    private const int SEED_FIELD_INDEX_STATE_BRANCH = 2;
    private const int SEED_FIELD_INDEX_SECTOR = 3;

    /// <summary>Encabezado que documenta el formato del archivo de datos.</summary>
    public static IReadOnlyCollection<string> FileHeaderLines { get; } = new[]
    {
        "# SB.API_SB - Base de datos de texto plano",
        "# Mantenimiento: Entidades Gubernamentales de la Republica Dominicana",
        "# Campos: Id|Nombre|Categoria|PoderDelEstado|Sector|Estado|CreadoEnUtc|CreadoPor|ActualizadoEnUtc|ActualizadoPor",
        "# Estado: 1 = Activo, 2 = Inactivo",
        "# El caracter | dentro de un valor se almacena escapado como \\p"
    };

    /// <summary>Serializa una entidad como linea del archivo de datos.</summary>
    /// <param name="entity">Entidad a serializar.</param>
    /// <returns>Linea de texto delimitada.</returns>
    public static string ToRecord(GovernmentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return FlatFileRecordSerializer.JoinFields(
            entity.Id.ToString(),
            entity.Name,
            entity.Category,
            entity.StateBranch,
            entity.Sector,
            ((int)entity.Status).ToString(CultureInfo.InvariantCulture),
            FlatFileRecordSerializer.FormatDateTime(entity.CreatedAt),
            entity.CreatedBy,
            FlatFileRecordSerializer.FormatDateTime(entity.UpdatedAt),
            entity.UpdatedBy);
    }

    /// <summary>
    /// Interpreta una linea del archivo de datos como entidad de dominio.
    /// </summary>
    /// <param name="line">Linea leida del archivo.</param>
    /// <returns>La entidad interpretada, o nulo si la linea esta malformada.</returns>
    public static GovernmentEntity? FromRecord(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string[] fields = FlatFileRecordSerializer.SplitFields(line);

        if (fields.Length < FULL_RECORD_FIELD_COUNT)
        {
            return null;
        }

        if (!Guid.TryParse(fields[FIELD_INDEX_IDENTIFIER], out Guid identifier))
        {
            return null;
        }

        return new GovernmentEntity
        {
            Id = identifier,
            Name = fields[FIELD_INDEX_NAME],
            Category = fields[FIELD_INDEX_CATEGORY],
            StateBranch = fields[FIELD_INDEX_STATE_BRANCH],
            Sector = fields[FIELD_INDEX_SECTOR],
            Status = ParseStatus(fields[FIELD_INDEX_STATUS]),
            CreatedAt = FlatFileRecordSerializer.ParseDateTime(fields[FIELD_INDEX_CREATED_AT])
                ?? DateTime.UnixEpoch,
            CreatedBy = fields[FIELD_INDEX_CREATED_BY],
            UpdatedAt = FlatFileRecordSerializer.ParseDateTime(fields[FIELD_INDEX_UPDATED_AT]),
            UpdatedBy = string.IsNullOrWhiteSpace(fields[FIELD_INDEX_UPDATED_BY])
                ? null
                : fields[FIELD_INDEX_UPDATED_BY]
        };
    }

    /// <summary>
    /// Interpreta una linea del archivo semilla, que solo contiene los cuatro
    /// campos de negocio extraidos del listado oficial.
    /// </summary>
    /// <param name="line">Linea del archivo semilla.</param>
    /// <param name="createdAt">Fecha de creacion a asignar.</param>
    /// <param name="createdBy">Usuario de creacion a asignar.</param>
    /// <returns>La entidad interpretada, o nulo si la linea esta malformada.</returns>
    public static GovernmentEntity? FromSeedRecord(string line, DateTime createdAt, string createdBy)
    {
        ArgumentNullException.ThrowIfNull(line);

        string[] fields = FlatFileRecordSerializer.SplitFields(line);

        if (fields.Length < SEED_RECORD_FIELD_COUNT ||
            string.IsNullOrWhiteSpace(fields[SEED_FIELD_INDEX_NAME]))
        {
            return null;
        }

        return new GovernmentEntity
        {
            Id = Guid.NewGuid(),
            Name = fields[SEED_FIELD_INDEX_NAME].Trim(),
            Category = fields[SEED_FIELD_INDEX_CATEGORY].Trim(),
            StateBranch = fields[SEED_FIELD_INDEX_STATE_BRANCH].Trim(),
            Sector = fields[SEED_FIELD_INDEX_SECTOR].Trim(),
            Status = RecordStatus.Active,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };
    }

    private static RecordStatus ParseStatus(string value) =>
        int.TryParse(value, CultureInfo.InvariantCulture, out int numericValue) &&
        Enum.IsDefined(typeof(RecordStatus), numericValue)
            ? (RecordStatus)numericValue
            : RecordStatus.Active;
}
