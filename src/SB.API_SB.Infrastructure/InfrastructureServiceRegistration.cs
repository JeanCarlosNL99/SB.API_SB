using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Application.Interfaces.EventLog;
using SB.API_SB.Infrastructure.Common;
using SB.API_SB.Infrastructure.EventLog;
using SB.API_SB.Infrastructure.FlatFileStorage;
using SB.API_SB.Infrastructure.Options;
using SB.API_SB.Infrastructure.Persistence;
using SB.API_SB.Infrastructure.Persistence.Repositories;
using SB.API_SB.Infrastructure.Persistence.Seeding;
using SB.API_SB.Infrastructure.Security;

namespace SB.API_SB.Infrastructure;

/// <summary>
/// Registro de las dependencias de la capa de Infraestructura.
/// </summary>
/// <remarks>
/// Cada capa expone su propio metodo de registro para que la capa de
/// Presentacion no tenga que conocer las clases concretas. Cambiar el proveedor
/// de base de datos o la implementacion de un repositorio se resuelve aqui, en un
/// unico archivo.
/// </remarks>
public static class InfrastructureServiceRegistration
{
    /// <summary>Nombre de la cadena de conexion de SQL Server en AppSettings.json.</summary>
    public const string SQL_SERVER_CONNECTION_NAME = "SqlServerConnection";

    /// <summary>Nombre de la cadena de conexion de SQLite en AppSettings.json.</summary>
    public const string SQLITE_CONNECTION_NAME = "SqliteConnection";

    /// <summary>
    /// Agrega al contenedor la persistencia relacional, la persistencia en archivo
    /// de texto plano y los servicios de seguridad.
    /// </summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <param name="configuration">Configuracion de la aplicacion.</param>
    /// <returns>La coleccion de servicios, para permitir encadenamiento.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptionsSections(configuration);
        services.AddCommonServices();
        services.AddRelationalPersistence(configuration);
        services.AddFlatFilePersistence();
        services.AddEventLogAccess();
        services.AddSecurityServices();

        return services;
    }

    private static void AddOptionsSections(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SECTION_NAME))
            .ValidateOnStart();

        services
            .AddOptions<FlatFileDatabaseOptions>()
            .Bind(configuration.GetSection(FlatFileDatabaseOptions.SECTION_NAME))
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SECTION_NAME))
            .ValidateOnStart();

        services
            .AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SECTION_NAME))
            .ValidateOnStart();

        services
            .AddOptions<EventLogOptions>()
            .Bind(configuration.GetSection(EventLogOptions.SECTION_NAME))
            .ValidateOnStart();
    }

    private static void AddCommonServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
    }

    private static void AddRelationalPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        DatabaseOptions databaseOptions = configuration
            .GetSection(DatabaseOptions.SECTION_NAME)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        services.AddDbContext<ApplicationDbContext>(contextOptions =>
        {
            ConfigureDatabaseProvider(contextOptions, configuration, databaseOptions);

            if (databaseOptions.EnableSensitiveDataLogging)
            {
                contextOptions.EnableSensitiveDataLogging();
                contextOptions.EnableDetailedErrors();
            }
        });

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IPayrollRunRepository, PayrollRunRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<PayrollHistorySeeder>();
        services.AddScoped<DatabaseInitializer>();
    }

    private static void ConfigureDatabaseProvider(
        DbContextOptionsBuilder contextOptions,
        IConfiguration configuration,
        DatabaseOptions databaseOptions)
    {
        bool useSqlite = string.Equals(
            databaseOptions.Provider,
            DatabaseProviderNames.SQLITE,
            StringComparison.OrdinalIgnoreCase);

        if (useSqlite)
        {
            string sqliteConnectionString = ResolveConnectionString(
                configuration,
                SQLITE_CONNECTION_NAME);

            contextOptions.UseSqlite(sqliteConnectionString);

            return;
        }

        string sqlServerConnectionString = ResolveConnectionString(
            configuration,
            SQL_SERVER_CONNECTION_NAME);

        contextOptions.UseSqlServer(
            sqlServerConnectionString,
            sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());
    }

    private static string ResolveConnectionString(
        IConfiguration configuration,
        string connectionName)
    {
        string? connectionString = configuration.GetConnectionString(connectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"La cadena de conexion '{connectionName}' no esta definida en AppSettings.json.");
        }

        return connectionString;
    }

    private static void AddFlatFilePersistence(this IServiceCollection services)
    {
        // Se registra como singleton porque la clase mantiene la cache en memoria y
        // el semaforo que serializa el acceso al archivo: ambos deben ser unicos
        // para toda la aplicacion.
        services.AddSingleton<IGovernmentEntityRepository, GovernmentEntityFileRepository>();
        services.AddSingleton<GovernmentEntityFileInitializer>();
    }

    private static void AddEventLogAccess(this IServiceCollection services)
    {
        // La lectura del registro no mantiene estado: se registra como singleton
        // porque abre y cierra el archivo en cada consulta.
        services.AddSingleton<IEventLogReader, SerilogFileEventLogReader>();
    }

    private static void AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
    }
}
