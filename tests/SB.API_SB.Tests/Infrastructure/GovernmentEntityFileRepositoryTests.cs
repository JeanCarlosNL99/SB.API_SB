using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Infrastructure.FlatFileStorage;
using SB.API_SB.Infrastructure.Options;
using SB.API_SB.Tests.TestDoubles;
using Xunit;

namespace SB.API_SB.Tests.Infrastructure;

/// <summary>
/// Pruebas del repositorio respaldado por archivo de texto plano.
/// </summary>
/// <remarks>
/// Se ejecutan contra un directorio temporal real: el objetivo es verificar que
/// los datos sobreviven al viaje completo por el sistema de archivos, no simular
/// el sistema de archivos. Cada prueba usa su propio directorio y lo elimina al
/// terminar, por lo que son reproducibles y aisladas entre si.
/// </remarks>
public sealed class GovernmentEntityFileRepositoryTests : IDisposable
{
    private const string CREATION_USER_NAME = "pruebas";

    private readonly string temporaryDirectoryPath;
    private readonly GovernmentEntityFileRepository repository;

    public GovernmentEntityFileRepositoryTests()
    {
        temporaryDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "SB.API_SB.Tests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(temporaryDirectoryPath);

        FlatFileDatabaseOptions options = new()
        {
            GovernmentEntitiesFilePath = "GovernmentEntities.txt",
            GovernmentEntitiesSeedFilePath = "GovernmentEntities.seed.txt",
            BackupDirectoryPath = "Backups",
            CreateBackupOnWrite = false
        };

        repository = new GovernmentEntityFileRepository(
            Microsoft.Extensions.Options.Options.Create(options),
            new TemporaryDirectoryPathResolver(temporaryDirectoryPath),
            NullLogger<GovernmentEntityFileRepository>.Instance);
    }

    [Fact]
    public async Task AddAsync_YLuegoGetByIdAsync_DevuelveLaEntidadPersistida()
    {
        GovernmentEntity entity = BuildEntity("Ministerio de Hacienda", "Ministerio", "Hacienda");

        await repository.AddAsync(entity);

        GovernmentEntity? persistedEntity = await repository.GetByIdAsync(entity.Id);

        Assert.NotNull(persistedEntity);
        Assert.Equal(entity.Name, persistedEntity!.Name);
        Assert.Equal(entity.Category, persistedEntity.Category);
        Assert.Equal(entity.Sector, persistedEntity.Sector);
        Assert.Equal(RecordStatus.Active, persistedEntity.Status);
    }

    [Fact]
    public async Task AddAsync_NombreConCaracteresEspecialesYAcentos_SeConservaAlLeer()
    {
        const string COMPLEX_NAME = "Direccion General de Aduanas | Área de Fiscalización";

        GovernmentEntity entity = BuildEntity(COMPLEX_NAME, "Ministerio", "Hacienda");

        await repository.AddAsync(entity);

        GovernmentEntity? persistedEntity = await repository.GetByIdAsync(entity.Id);

        Assert.NotNull(persistedEntity);
        Assert.Equal(COMPLEX_NAME, persistedEntity!.Name);
    }

    [Fact]
    public async Task UpdateAsync_ModificaSoloLaEntidadIndicada()
    {
        GovernmentEntity firstEntity = BuildEntity("Entidad Uno", "Ministerio", "Hacienda");
        GovernmentEntity secondEntity = BuildEntity("Entidad Dos", "Ministerio", "Salud");

        await repository.AddAsync(firstEntity);
        await repository.AddAsync(secondEntity);

        firstEntity.Sector = "Educacion";
        firstEntity.Status = RecordStatus.Inactive;

        await repository.UpdateAsync(firstEntity);

        GovernmentEntity? updatedEntity = await repository.GetByIdAsync(firstEntity.Id);
        GovernmentEntity? untouchedEntity = await repository.GetByIdAsync(secondEntity.Id);

        Assert.Equal("Educacion", updatedEntity!.Sector);
        Assert.Equal(RecordStatus.Inactive, updatedEntity.Status);
        Assert.Equal("Salud", untouchedEntity!.Sector);
        Assert.Equal(RecordStatus.Active, untouchedEntity.Status);
    }

    [Fact]
    public async Task DeleteAsync_EliminaLaEntidadDelArchivo()
    {
        GovernmentEntity entity = BuildEntity("Entidad Temporal", "Ministerio", "Hacienda");

        await repository.AddAsync(entity);
        await repository.DeleteAsync(entity);

        GovernmentEntity? deletedEntity = await repository.GetByIdAsync(entity.Id);
        IReadOnlyCollection<GovernmentEntity> remainingEntities = await repository.GetAllAsync();

        Assert.Null(deletedEntity);
        Assert.Empty(remainingEntities);
    }

    [Fact]
    public async Task SearchAsync_FiltraPorNombreParcialSinDistinguirMayusculas()
    {
        await repository.AddAsync(BuildEntity("Banco Central", "Organismo", "Hacienda"));
        await repository.AddAsync(BuildEntity("Banco Agricola", "Empresa Publica", "Agricultura"));
        await repository.AddAsync(BuildEntity("Acuario Nacional", "Organismo", "Medio Ambiente"));

        var searchResult = await repository.SearchAsync(new GovernmentEntityFilterCriteria
        {
            Name = "banco"
        });

        Assert.Equal(2, searchResult.TotalCount);
        Assert.All(searchResult.Items, entity => Assert.Contains("Banco", entity.Name));
    }

    [Fact]
    public async Task SearchAsync_ConVariosFiltros_LosCombinaTodos()
    {
        await repository.AddAsync(BuildEntity("Entidad A", "Ministerio", "Hacienda"));
        await repository.AddAsync(BuildEntity("Entidad B", "Ministerio", "Salud"));
        await repository.AddAsync(BuildEntity("Entidad C", "Organismo", "Hacienda"));

        var searchResult = await repository.SearchAsync(new GovernmentEntityFilterCriteria
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
        for (int index = 1; index <= 25; index++)
        {
            await repository.AddAsync(BuildEntity(
                $"Entidad {index:D2}",
                "Ministerio",
                "Hacienda"));
        }

        var secondPage = await repository.SearchAsync(new GovernmentEntityFilterCriteria
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

    [Fact]
    public async Task ExistsByNameAsync_IgnoraMayusculasYPuedeExcluirUnRegistro()
    {
        GovernmentEntity entity = BuildEntity("Ministerio de Salud", "Ministerio", "Salud");

        await repository.AddAsync(entity);

        Assert.True(await repository.ExistsByNameAsync("ministerio de salud"));
        Assert.False(await repository.ExistsByNameAsync("Ministerio de Salud", entity.Id));
        Assert.False(await repository.ExistsByNameAsync("Ministerio de Educacion"));
    }

    [Fact]
    public async Task GetCatalogsAsync_DevuelveValoresDistintosOrdenados()
    {
        await repository.AddAsync(BuildEntity("Entidad A", "Ministerio", "Salud"));
        await repository.AddAsync(BuildEntity("Entidad B", "Ministerio", "Hacienda"));
        await repository.AddAsync(BuildEntity("Entidad C", "Organismo", "Salud"));

        GovernmentEntityCatalogs catalogs = await repository.GetCatalogsAsync();

        Assert.Equal(new[] { "Ministerio", "Organismo" }, catalogs.Categories);
        Assert.Equal(new[] { "Hacienda", "Salud" }, catalogs.Sectors);
        Assert.Single(catalogs.StateBranches);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        repository.Dispose();

        if (Directory.Exists(temporaryDirectoryPath))
        {
            Directory.Delete(temporaryDirectoryPath, recursive: true);
        }
    }

    private static GovernmentEntity BuildEntity(string name, string category, string sector) =>
        new()
        {
            Name = name,
            Category = category,
            StateBranch = "Poder Ejecutivo",
            Sector = sector,
            Status = RecordStatus.Active,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = CREATION_USER_NAME
        };
}
