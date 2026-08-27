using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Infrastructure;
using SB.API_SB.Infrastructure.EventLog;
using SB.API_SB.Infrastructure.FlatFileStorage;
using SB.API_SB.Infrastructure.Persistence.Seeding;
using SB.API_SB.Presentation.Configuration;
using SB.API_SB.Presentation.Middleware;
using SB.API_SB.Presentation.Security;
using SB.API_SB.Services;
using Serilog;

// Serilog se inicializa antes que el host para que cualquier fallo durante el
// arranque quede registrado en lugar de perderse en la consola.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando SB.API_SB.");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();

    // Composicion de dependencias: cada capa expone su propio registro y la capa
    // de Presentacion solo las encadena. Esta es la unica clase que conoce a
    // todas las capas.
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddPresentationServices(builder.Configuration);

    WebApplication application = builder.Build();

    await application.InitializeDataStoresAsync();

    application.ConfigureRequestPipeline();

    await application.RunAsync();

    return 0;
}
catch (Exception exception)
{
    Log.Fatal(exception, "La aplicacion termino de forma inesperada durante el arranque.");

    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Metodos de extension que agrupan la configuracion de la capa de Presentacion.
/// Mantenerlos separados deja el archivo de arranque legible de principio a fin.
/// </summary>
internal static class PresentationStartupExtensions
{
    /// <summary>Registra los servicios propios de la capa de Presentacion.</summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <param name="configuration">Configuracion de la aplicacion.</param>
    /// <returns>La coleccion de servicios, para permitir encadenamiento.</returns>
    public static IServiceCollection AddPresentationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        services.AddSingleton<IFlatFilePathResolver, FlatFilePathResolver>();
        services.AddSingleton<IEventLogPathResolver, EventLogPathResolver>();

        services
            .AddControllers(mvcOptions =>
            {
                // El filtro de validacion se aplica a todos los controladores, de
                // modo que ninguna accion pueda olvidarse de validar su entrada.
                mvcOptions.Filters.Add<RequestValidationFilter>();
            })
            .AddJsonOptions(jsonOptions =>
            {
                // Las enumeraciones viajan como texto para que el cliente React no
                // dependa de valores numericos.
                jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;
            });

        services.Configure<ApiBehaviorOptions>(apiBehaviorOptions =>
        {
            // Los errores de enlace de modelo se devuelven con el mismo formato
            // ProblemDetails que el resto de los errores de la API.
            apiBehaviorOptions.SuppressMapClientErrors = false;
        });

        services.AddJwtAuthentication(configuration);
        services.AddClientApplicationCors(configuration);
        services.AddApiDocumentation();

        return services;
    }

    /// <summary>
    /// Prepara la base de datos relacional y la base de datos de texto plano antes
    /// de aceptar peticiones.
    /// </summary>
    /// <param name="application">Aplicacion web construida.</param>
    public static async Task InitializeDataStoresAsync(this WebApplication application)
    {
        using IServiceScope serviceScope = application.Services.CreateScope();

        DatabaseInitializer initializer = serviceScope.ServiceProvider
            .GetRequiredService<DatabaseInitializer>();

        await initializer.InitializeAsync();
    }

    /// <summary>Configura la tuberia de middlewares en el orden correcto.</summary>
    /// <param name="application">Aplicacion web construida.</param>
    public static void ConfigureRequestPipeline(this WebApplication application)
    {
        // El manejo de excepciones va primero para poder capturar los fallos de
        // cualquier middleware posterior.
        application.UseMiddleware<ExceptionHandlingMiddleware>();

        application.UseRequestLogging();

        application.UseApiDocumentation();

        application.UseCors(CorsConfiguration.CLIENT_APPLICATION_POLICY_NAME);

        application.UseAuthentication();
        application.UseAuthorization();

        application.MapControllers();
    }
}
