using SB.API_SB.Infrastructure.FlatFileStorage;
using Xunit;

namespace SB.API_SB.Tests.Infrastructure;

/// <summary>
/// Pruebas de la derivacion de identificadores de entidades gubernamentales.
/// </summary>
/// <remarks>
/// Estas garantias son las que sostienen la asociacion de empleados y nominas a
/// entidades: el archivo de datos se regenera a partir del archivo semilla y no
/// se versiona, de modo que si los identificadores cambiaran al regenerarlo, las
/// referencias ya registradas apuntarian a entidades inexistentes sin que nada
/// avisara.
/// </remarks>
public sealed class DeterministicIdentifierFactoryTests
{
    private const string ENTITY_NAME = "Direccion General de Impuestos Internos";

    [Fact]
    public void ForGovernmentEntity_MismoNombre_DevuelveElMismoIdentificador()
    {
        Guid firstIdentifier = DeterministicIdentifierFactory.ForGovernmentEntity(ENTITY_NAME);
        Guid secondIdentifier = DeterministicIdentifierFactory.ForGovernmentEntity(ENTITY_NAME);

        Assert.Equal(firstIdentifier, secondIdentifier);
    }

    [Fact]
    public void ForGovernmentEntity_NombresDistintos_DevuelveIdentificadoresDistintos()
    {
        Guid firstIdentifier = DeterministicIdentifierFactory.ForGovernmentEntity(ENTITY_NAME);
        Guid secondIdentifier = DeterministicIdentifierFactory.ForGovernmentEntity(
            "Tesoreria Nacional");

        Assert.NotEqual(firstIdentifier, secondIdentifier);
    }

    /// <summary>
    /// Una diferencia de formato en el archivo semilla no debe producir una
    /// entidad distinta.
    /// </summary>
    [Theory]
    [InlineData("  Direccion General de Impuestos Internos  ")]
    [InlineData("direccion general de impuestos internos")]
    [InlineData("DIRECCION GENERAL DE IMPUESTOS INTERNOS")]
    public void ForGovernmentEntity_IgnoraEspaciosSobrantesYMayusculas(string variantName)
    {
        Guid expectedIdentifier = DeterministicIdentifierFactory.ForGovernmentEntity(ENTITY_NAME);

        Guid actualIdentifier = DeterministicIdentifierFactory.ForGovernmentEntity(variantName);

        Assert.Equal(expectedIdentifier, actualIdentifier);
    }

    [Fact]
    public void ForGovernmentEntity_NombreVacio_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(
            () => DeterministicIdentifierFactory.ForGovernmentEntity("   "));
    }

    [Fact]
    public void ForGovernmentEntity_NoDevuelveElIdentificadorVacio()
    {
        Guid identifier = DeterministicIdentifierFactory.ForGovernmentEntity(ENTITY_NAME);

        Assert.NotEqual(Guid.Empty, identifier);
    }
}
