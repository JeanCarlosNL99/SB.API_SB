using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SB.API_SB.Infrastructure.FlatFileStorage;
using SB.API_SB.Infrastructure.Options;

namespace SB.API_SB.Infrastructure.Persistence.Seeding;

/// <summary>
/// Prepara los dos almacenes de datos de la solucion al iniciar la aplicacion:
/// la base de datos relacional (esquema y datos base) y la base de datos de texto
/// plano de las entidades gubernamentales.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly ApplicationDbContext databaseContext;
    private readonly DatabaseSeeder databaseSeeder;
    private readonly PayrollHistorySeeder payrollHistorySeeder;
    private readonly GovernmentEntityFileInitializer flatFileInitializer;
    private readonly DatabaseOptions options;
    private readonly ILogger<DatabaseInitializer> logger;

    public DatabaseInitializer(
        ApplicationDbContext databaseContext,
        DatabaseSeeder databaseSeeder,
        PayrollHistorySeeder payrollHistorySeeder,
        GovernmentEntityFileInitializer flatFileInitializer,
        IOptions<DatabaseOptions> options,
        ILogger<DatabaseInitializer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.databaseContext = databaseContext;
        this.databaseSeeder = databaseSeeder;
        this.payrollHistorySeeder = payrollHistorySeeder;
        this.flatFileInitializer = flatFileInitializer;
        this.options = options.Value;
        this.logger = logger;
    }

    /// <summary>Crea el esquema si hace falta y siembra los datos iniciales.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // La base de datos de texto plano se prepara siempre: es el mantenimiento
        // exigido por el requerimiento y no depende del motor relacional.
        await flatFileInitializer.InitializeAsync(cancellationToken);

        if (!options.ApplyAutomaticInitialization)
        {
            logger.LogInformation(
                "La inicializacion automatica de la base de datos relacional esta deshabilitada.");

            return;
        }

        bool schemaWasCreated = await databaseContext.Database
            .EnsureCreatedAsync(cancellationToken);

        logger.LogInformation(
            "Base de datos relacional lista. Esquema creado en este arranque: {SchemaWasCreated}.",
            schemaWasCreated);

        await databaseSeeder.SeedAsync(cancellationToken);

        // El historial de nomina se siembra despues de los empleados porque se
        // calcula a partir de ellos.
        await payrollHistorySeeder.SeedAsync(cancellationToken);
    }
}
