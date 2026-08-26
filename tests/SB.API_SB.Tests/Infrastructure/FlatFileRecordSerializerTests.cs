using SB.API_SB.Infrastructure.FlatFileStorage;
using Xunit;

namespace SB.API_SB.Tests.Infrastructure;

/// <summary>
/// Pruebas del formato de la base de datos de texto plano.
/// </summary>
/// <remarks>
/// El escape es la garantia de que un dato del usuario no puede corromper el
/// archivo. Se verifica el viaje completo (serializar y volver a leer) con los
/// caracteres que romperian el formato si no se escaparan.
/// </remarks>
public sealed class FlatFileRecordSerializerTests
{
    [Theory]
    [InlineData("Ministerio de Hacienda")]
    [InlineData("Nombre con | delimitador")]
    [InlineData("Nombre con \\ barra invertida")]
    [InlineData("Nombre con \\p secuencia parecida al escape")]
    [InlineData("Nombre con salto\nde linea")]
    [InlineData("Direccion General de Impuestos Internos (DGII)")]
    [InlineData("")]
    public void JoinFields_YLuegoSplitFields_DevuelveLosValoresOriginales(string value)
    {
        string record = FlatFileRecordSerializer.JoinFields(value, "segundo campo", "tercer campo");

        string[] fields = FlatFileRecordSerializer.SplitFields(record);

        Assert.Equal(3, fields.Length);
        Assert.Equal(value, fields[0]);
        Assert.Equal("segundo campo", fields[1]);
        Assert.Equal("tercer campo", fields[2]);
    }

    [Fact]
    public void JoinFields_ValorConDelimitador_NoDejaElDelimitadorSinEscapar()
    {
        string record = FlatFileRecordSerializer.JoinFields("A|B", "C");

        // Solo debe quedar un delimitador real: el que separa los dos campos.
        int delimiterCount = record.Count(
            character => character == FlatFileRecordSerializer.FIELD_DELIMITER);

        Assert.Equal(1, delimiterCount);
        Assert.Equal(2, FlatFileRecordSerializer.SplitFields(record).Length);
    }

    [Theory]
    [InlineData("# comentario", true)]
    [InlineData("   # comentario con espacios", true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("dato|dato", false)]
    public void IsIgnorableLine_IdentificaComentariosYLineasVacias(
        string line,
        bool expectedResult)
    {
        Assert.Equal(expectedResult, FlatFileRecordSerializer.IsIgnorableLine(line));
    }

    [Fact]
    public void FormatDateTime_YLuegoParseDateTime_ConservaLaFechaEnUtc()
    {
        DateTime originalValue = new(2026, 8, 26, 14, 35, 12, DateTimeKind.Utc);

        string formattedValue = FlatFileRecordSerializer.FormatDateTime(originalValue);
        DateTime? parsedValue = FlatFileRecordSerializer.ParseDateTime(formattedValue);

        Assert.Equal(originalValue, parsedValue);
        Assert.Equal(DateTimeKind.Utc, parsedValue!.Value.Kind);
    }

    [Fact]
    public void ParseDateTime_ValorVacioOInvalido_DevuelveNulo()
    {
        Assert.Null(FlatFileRecordSerializer.ParseDateTime(string.Empty));
        Assert.Null(FlatFileRecordSerializer.ParseDateTime("no es una fecha"));
    }
}
