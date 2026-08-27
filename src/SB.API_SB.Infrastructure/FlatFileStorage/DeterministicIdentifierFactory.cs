using System.Security.Cryptography;
using System.Text;

namespace SB.API_SB.Infrastructure.FlatFileStorage;

/// <summary>
/// Deriva identificadores estables a partir del nombre oficial de una entidad
/// gubernamental.
/// </summary>
/// <remarks>
/// La base de datos de texto plano se genera en el primer arranque a partir del
/// archivo semilla, y no se versiona. Si los identificadores se generaran al azar,
/// regenerar ese archivo produciria identificadores nuevos y los empleados y las
/// nominas ya registrados quedarian apuntando a entidades que no existen: la
/// referencia se romperia sin que nada avisara.
/// <para>
/// Derivar el identificador del nombre resuelve el problema de raiz. El mismo
/// nombre produce siempre el mismo identificador, de modo que el archivo se puede
/// borrar y regenerar sin perder las asociaciones. Es la misma idea que un
/// identificador de nombre segun la RFC 4122.
/// </para>
/// <para>
/// La funcion hash se usa como funcion de derivacion, no como primitiva de
/// seguridad: no protege ningun secreto, solo garantiza que un nombre produzca
/// siempre el mismo identificador.
/// </para>
/// </remarks>
public static class DeterministicIdentifierFactory
{
    /// <summary>
    /// Espacio de nombres del mantenimiento de entidades gubernamentales. Aisla
    /// estos identificadores de los que pudiera derivar cualquier otro catalogo.
    /// </summary>
    private const string GOVERNMENT_ENTITY_NAMESPACE = "SB.API_SB.GovernmentEntity";

    private const int GUID_BYTE_COUNT = 16;
    private const int VERSION_BYTE_INDEX = 7;
    private const int VARIANT_BYTE_INDEX = 8;

    /// <summary>
    /// Calcula el identificador que corresponde al nombre indicado. La
    /// comparacion es insensible a mayusculas y a espacios sobrantes para que una
    /// diferencia de formato en el archivo semilla no cambie el identificador.
    /// </summary>
    /// <param name="governmentEntityName">Nombre oficial de la entidad.</param>
    /// <returns>Identificador estable derivado del nombre.</returns>
    public static Guid ForGovernmentEntity(string governmentEntityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(governmentEntityName);

        string normalizedName = governmentEntityName.Trim().ToUpperInvariant();
        string seedText = $"{GOVERNMENT_ENTITY_NAMESPACE}:{normalizedName}";

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(seedText));
        byte[] identifierBytes = new byte[GUID_BYTE_COUNT];

        Array.Copy(digest, identifierBytes, GUID_BYTE_COUNT);

        // Se marcan los bits de version y variante para que el valor resultante sea
        // un identificador valido y no se confunda con uno generado al azar.
        identifierBytes[VERSION_BYTE_INDEX] =
            (byte)((identifierBytes[VERSION_BYTE_INDEX] & 0x0F) | 0x80);
        identifierBytes[VARIANT_BYTE_INDEX] =
            (byte)((identifierBytes[VARIANT_BYTE_INDEX] & 0x3F) | 0x80);

        return new Guid(identifierBytes);
    }
}
