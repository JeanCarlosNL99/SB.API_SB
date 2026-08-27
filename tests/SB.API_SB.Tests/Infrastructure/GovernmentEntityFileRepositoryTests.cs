using Microsoft.Extensions.Logging.Abstractions;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Infrastructure.FlatFileStorage;
using SB.API_SB.Infrastructure.Options;
using SB.API_SB.Tests.TestDoubles;
using Xunit;

namespace SB.API_SB.Tests.Infrastructure;

/// <summary>
/// Pruebas del repositorio de consulta respaldado por archivo de texto plano.
/// </summary>
/// <remarks>
/// Se ejercita el recorrido completo del dato: se escribe un archivo semilla, el
/// inicializador genera el archivo de datos y el repositorio lo lee. Verificar
/// ese camino y no una escritura sintetica es lo que da valor a la prueba, porque
/// es exactamente lo que ocurre al arrancar la aplicacion.
/// <para>
/// Cada prueba usa su propio directorio temporal real y lo elimina al terminar,
/// por lo que son reproducibles y aisladas entre si.
/// </para>
/// </remarks>
public sealed class GovernmentEntityFileRepositoryTests : IDisposable
{
    private const string DATA_FILE_NAME = "GovernmentEntities.txt";
    private const string SEED_FILE_NAME = "GovernmentEntities.seed.txt";
    private const string STATE_BRANCH = "Poder Ejecutivo";

    private readonly string temporaryDirectoryPath;
    private readonly FlatFileDatabaseOptions options;
    private readonly TemporaryDirectoryPathResolver pathResolver;

    public GovernmentEntityFileRepositoryTests()
    {
        temporaryDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "SB.API_SB.Tests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(temporaryDirectoryPath);

        options = new FlatFileDatabaseOptions
        {
            GovernmentEntitiesFilePath = DATA_FILE_NAME,
            GovernmentEntitiesSeedFilePath = SEED_FILE_NAME
        };

        pathResolver = new TemporaryDirectoryPathResolver(temporaryDirectoryPath);
    }

    [Fact]
    public async Task GetByIdAsync_DevuelveLaEntidadSembradaConTodosSusCampos()
    {
        GovernmentEntityFileRepository repository = await BuildSeededRepositoryAsync(
            "Ministerio de Hacienda|Ministerio|Poder Ejecutivo|Hacienda");

        GovernmentEntity expectedEntity = (await repository.GetAllAsync()).Single();

        GovernmentEntity? persistedEntity = await repository.GetByIdAsync(expectedEntity.Id);

        Assert.NotNull(persistedEntity);
        Assert.Equal("Ministerio de Hacienda", persistedEntity!.Name);
        Assert.Equal("Ministerio", persistedEntity.Category);
        Assert.Equal(STATE_BRANCH, persistedEntity.StateBranch);
        Assert.Equal("Hacienda", persistedEntity.Sector);
    }

    [Fact]
    public async Task GetByIdAsync_IdentificadorInexistente_DevuelveNulo()
    {
        GovernmentEntityFileRepository repository = await BuildSeededRepositoryAsync(
            "Ministerio de Salud|Ministerio|Poder Ejecutivo|Salud");

        GovernmentEntity? missingEntity = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(missingEntity);
    }

    /// <summary>
    /// El listado oficial lleva acentos y comas. Si el escapado del archivo o la
    /// codificacion fallaran, el dato volveria distinto de como se escribio.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_NombreConAcentosYComas_SeConservaAlLeer()
    {
        const string ENTITY_NAME =
            "Direccion General de Migracion, Aduanas y Bienes Nacionales";

        GovernmentEntityFileRepository repository = await BuildSeededRepositoryAsync(
            $"{ENTITY_NAME}|Organismo Descentralizado|Poder Ejecutivo|Interior");

        GovernmentEntity persistedEntity = (await repository.GetAllAsync()).Single();

        Assert.Equal(ENTITY_NAME, persistedEntity.Name);
    }

    /// <summary>
    /// Es la garantia que sostiene la asociacion de empleados a entidades: el
    /// archivo de datos se puede borrar y regenerar sin que cambie ningun
    /// identificador, porque se derivan del nombre.
    /// </summary>
    [Fact]
    public async Task RegenerarElArchivo_ProduceLosMismosIdentificadores()
    {
        const string SEED_CONTENT = "Tesoreria Nacional|Organismo|Poder Ejecutivo|Hacienda";

        GovernmentEntityFileRepository firstRepository =
            await BuildSeededRepositoryAsync(SEED_CONTENT);

        Guid identifierBeforeRegeneration = (await firstRepository.GetAllAsync()).Single().Id;

        firstRepository.Dispose();
        File.Delete(Path.Combine(temporaryDirectoryPath, DATA_FILE_NAME));

        GovernmentEntityFileRepository secondRepository =
            await BuildSeededRepositoryAsync(SEED_CONTENT);

        Guid identifierAfterRegeneration = (await secondRepository.GetAllAsync()).Single().Id;

        Assert.Equal(identifierBeforeRegeneration, identifierAfterRegeneration);
    }

    [Fact]
    public async Task SearchAsync_FiltraPorNombreParcialSinDistinguirMayusculas()
    {
        GovernmentEntityFileRepository repository = await BuildSeededRepositoryAsync(
            "Banco Central|Organismo|Poder Ejecutivo|Hacienda",
            "Banco Agricola|Empresa Publica|Poder Ejecutivo|Agricultura",
            "Acuario Nacional|Organismo|Poder Ejecutivo|Medio Ambiente");

        PagedList<GovernmentEntity> searchResult = await repository.SearchAsync(
            new GovernmentEntityFilterCriteria { Name = "banco" });

        Assert.Equal(2, searchResult.TotalCount);
        Assert.All(searchResult.Items, entity => Assert.Contains("Banco", entity.Name));
    }

    [Fact]
    public async Task SearchAsync_ConVariosFiltros_LosCombinaTodos()
    {
        GovernmentEntityFileRepository repository = await BuildSeededRepositoryAsync(
            "Entidad A|Ministerio|Poder Ejecutivo|Hacienda",
            "Entidad B|Ministerio|Poder Ejecutivo|Salud",
            "Entidad C|Organismo|Poder Ejecutivo|Hacienda");

        PagedList<GovernmentEntity> searchResult = await repository.SearchAsync(
            new GovernmentEntityFilterCriteria
            {
                Category = "Ministerio",
                Sector = "Hacienda"
            });

        Assert.Equal(1, searchResult.TotalCount);
        Assert.Equal("Entidad A", searchResult.Items.Single().Name);
    }

    [Fact]
    public async Task SearchAsync_DevuelveSoloLaPaginaSolicitada()
    {
        string[] seedLines = Enumerable.Range(1, 25)
            .Select(index => $"Entidad {index:D2}|Ministerio|Poder Ejecutivo|Hacienda")
            .ToArray();

        GovernmentEntityFileRepository repository =
            await BuildSeededRepositoryAsync(seedLines);

        PagedList<GovernmentEntity> secondPage = await repository.SearchAsync(
            new GovernmentEntityFilterCriteria
            {
                PageNumber = 2,
                PageSize = 10
            });

        Assert.Equal(25, secondPage.TotalCount);
        Assert.Equal(10, secondPage.Items.Count);
        Assert.Equal(3, secondPage.TotalPages);
        Assert.True(secondPage.HasNextPage);
        Assert.True(secondPage.HasPreviousPage);
    }

    /// <summary>
    /// Es la proyeccion que permite resolver el nombre de la entidad de una pagina
    /// completa de empleados con una sola lectura del catalogo.
    /// </summary>
    [Fact]
    public async Task GetNamesByIdentifierAsync_DevuelveElNombreDeCadaEntidad()
    {
        GovernmentEntityFileRepository repository = await BuildSeededRepositoryAsync(
            "Entidad A|Ministerio|Poder Ejecutivo|Hacienda",
            "Entidad B|Organismo|Poder Ejecutivo|Salud");

        IReadOnlyCollection<GovernmentEntity> entities = await repository.GetAllAsync();
        IReadOnlyDictionary<Guid, string> namesByIdentifier =
            await repository.GetNamesByIdentifierAsync();

        Assert.Equal(2, namesByIdentifier.Count);
        Assert.All(
            entities,
            entity => Assert.Equal(entity.Name, namesByIdentifier[entity.Id]));
    }

    [Fact]
    public async Task GetCatalogsAsync_DevuelveValoresDistintosOrdenados()
    {
        GovernmentEntityFileRepository repository = await BuildSeededRepositoryAsync(
            "Entidad A|Ministerio|Poder Ejecutivo|Salud",
            "Entidad B|Ministerio|Poder Ejecutivo|Hacienda",
            "Entidad C|Organismo|Poder Ejecutivo|Salud");

        GovernmentEntityCatalogs catalogs = await repository.GetCatalogsAsync();

        Assert.Equal(new[] { "Ministerio", "Organismo" }, catalogs.Categories);
        Assert.Equal(new[] { "Hacienda", "Salud" }, catalogs.Sectors);
        Assert.Single(catalogs.StateBranches);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectoryPath))
        {
            Directory.Delete(temporaryDirectoryPath, recursive: true);
        }
    }

    /// <summary>
    /// Escribe el archivo semilla indicado, deja que el inicializador genere el
    /// archivo de datos y devuelve un repositorio que lo lee.
    /// </summary>
    /// <param name="seedLines">Registros semilla, en el formato del listado oficial.</param>
    /// <returns>Repositorio listo para consultar.</returns>
    private async Task<GovernmentEntityFileRepository> BuildSeededRepositoryAsync(
        params string[] seedLines)
    {
        await File.WriteAllLinesAsync(
            Path.Combine(temporaryDirectoryPath, SEED_FILE_NAME),
            seedLines);

        GovernmentEntityFileInitializer initializer = new(
            Microsoft.Extensions.Options.Options.Create(options),
            pathResolver,
            new FixedDateTimeProvider(),
            NullLogger<GovernmentEntityFileInitializer>.Instance);

        await initializer.InitializeAsync();

        return new GovernmentEntityFileRepository(
            Microsoft.Extensions.Options.Options.Create(options),
            pathResolver,
            NullLogger<GovernmentEntityFileRepository>.Instance);
    }
}
