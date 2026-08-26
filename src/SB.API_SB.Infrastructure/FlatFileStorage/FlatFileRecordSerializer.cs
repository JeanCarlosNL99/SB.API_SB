using System.Globalization;
using System.Text;

namespace SB.API_SB.Infrastructure.FlatFileStorage;

/// <summary>
/// Serializador de registros delimitados para la base de datos de texto plano.
/// </summary>
/// <remarks>
/// El delimitador es la barra vertical. Para que un valor que contenga el
/// delimitador, una barra invertida o un salto de linea no rompa el formato, se
/// aplica un escape reversible: esa es la diferencia entre un archivo delimitado
/// confiable y una concatenacion de cadenas.
/// </remarks>
public static class FlatFileRecordSerializer
{
    /// <summary>Caracter que separa los campos de un registro.</summary>
    public const char FIELD_DELIMITER = '|';

    /// <summary>Caracter que introduce una secuencia de escape.</summary>
    public const char ESCAPE_CHARACTER = '\\';

    /// <summary>Prefijo que identifica una linea de comentario o encabezado.</summary>
    public const string COMMENT_PREFIX = "#";

    private const char ESCAPED_DELIMITER = 'p';
    private const char ESCAPED_LINE_FEED = 'n';
    private const char ESCAPED_CARRIAGE_RETURN = 'r';

    /// <summary>Une los campos de un registro en una unica linea de texto.</summary>
    /// <param name="fields">Campos del registro, en orden.</param>
    /// <returns>Linea serializada.</returns>
    public static string JoinFields(params string?[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        return string.Join(FIELD_DELIMITER, fields.Select(Escape));
    }

    /// <summary>Divide una linea de texto en los campos del registro.</summary>
    /// <param name="line">Linea serializada.</param>
    /// <returns>Campos del registro, ya sin escapes.</returns>
    public static string[] SplitFields(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return line
            .Split(FIELD_DELIMITER)
            .Select(Unescape)
            .ToArray();
    }

    /// <summary>Indica si la linea debe ignorarse al leer el archivo.</summary>
    /// <param name="line">Linea leida del archivo.</param>
    /// <returns>Verdadero si la linea esta vacia o es un comentario.</returns>
    public static bool IsIgnorableLine(string line) =>
        string.IsNullOrWhiteSpace(line) ||
        line.TrimStart().StartsWith(COMMENT_PREFIX, StringComparison.Ordinal);

    /// <summary>Formatea una fecha en un formato estable e independiente de la cultura.</summary>
    /// <param name="value">Fecha a formatear.</param>
    /// <returns>Fecha en formato ISO 8601 de ida y vuelta, o cadena vacia si es nula.</returns>
    public static string FormatDateTime(DateTime? value) =>
        value.HasValue
            ? value.Value.ToString("O", CultureInfo.InvariantCulture)
            : string.Empty;

    /// <summary>Interpreta una fecha almacenada en el archivo.</summary>
    /// <param name="value">Texto leido del archivo.</param>
    /// <returns>Fecha interpretada o nulo si el campo esta vacio.</returns>
    public static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime parsedValue)
            ? parsedValue
            : null;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder escapedValue = new(value.Length);

        foreach (char character in value)
        {
            switch (character)
            {
                case ESCAPE_CHARACTER:
                    escapedValue.Append(ESCAPE_CHARACTER).Append(ESCAPE_CHARACTER);
                    break;
                case FIELD_DELIMITER:
                    escapedValue.Append(ESCAPE_CHARACTER).Append(ESCAPED_DELIMITER);
                    break;
                case '\n':
                    escapedValue.Append(ESCAPE_CHARACTER).Append(ESCAPED_LINE_FEED);
                    break;
                case '\r':
                    escapedValue.Append(ESCAPE_CHARACTER).Append(ESCAPED_CARRIAGE_RETURN);
                    break;
                default:
                    escapedValue.Append(character);
                    break;
            }
        }

        return escapedValue.ToString();
    }

    private static string Unescape(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains(ESCAPE_CHARACTER))
        {
            return value;
        }

        StringBuilder unescapedValue = new(value.Length);

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != ESCAPE_CHARACTER || index == value.Length - 1)
            {
                unescapedValue.Append(value[index]);
                continue;
            }

            index++;

            unescapedValue.Append(value[index] switch
            {
                ESCAPED_DELIMITER => FIELD_DELIMITER,
                ESCAPED_LINE_FEED => '\n',
                ESCAPED_CARRIAGE_RETURN => '\r',
                ESCAPE_CHARACTER => ESCAPE_CHARACTER,
                _ => value[index]
            });
        }

        return unescapedValue.ToString();
    }
}
