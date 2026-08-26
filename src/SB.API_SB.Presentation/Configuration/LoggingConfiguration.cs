using Serilog;
using Serilog.Events;

namespace SB.API_SB.Presentation.Configuration;

/// <summary>
/// Configuracion del registro de eventos con Serilog.
/// </summary>
/// <remarks>
/// Serilog se configura por completo desde AppSettings.json (seccion
/// <c>Serilog</c>), lo que permite cambiar niveles y destinos sin recompilar. Se
/// escribe a consola y a archivos diarios: los archivos son la evidencia
/// consultable de "todo lo que pasa" en la aplicacion, y se enriquece cada evento
/// con el nombre de la maquina y el identificador del hilo para poder rastrear el
/// origen de un problema.
/// </remarks>
public static class LoggingConfiguration
{
    private const string CORRELATION_IDENTIFIER_PROPERTY = "CorrelationId";
    private const string USER_NAME_PROPERTY = "UserName";
    private const string CLIENT_ADDRESS_PROPERTY = "ClientAddress";
    private const string REQUEST_LOG_MESSAGE_TEMPLATE =
        "{RequestMethod} {RequestPath} respondio {StatusCode} en {Elapsed:0.0000} ms";

    /// <summary>
    /// Reemplaza el proveedor de registro predeterminado por Serilog, leyendo su
    /// configuracion del archivo de configuracion de la aplicacion.
    /// </summary>
    /// <param name="builder">Constructor de la aplicacion web.</param>
    /// <returns>El constructor, para permitir encadenamiento.</returns>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseSerilog((hostContext, serviceProvider, loggerConfiguration) =>
            loggerConfiguration
                .ReadFrom.Configuration(hostContext.Configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "SB.API_SB"));

        return builder;
    }

    /// <summary>
    /// Activa el registro resumido de cada peticion HTTP, con una sola linea por
    /// peticion en lugar de las multiples que emite el registro predeterminado.
    /// </summary>
    /// <param name="application">Aplicacion web en construccion.</param>
    /// <returns>La aplicacion, para permitir encadenamiento.</returns>
    public static WebApplication UseRequestLogging(this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.UseSerilogRequestLogging(requestLoggingOptions =>
        {
            requestLoggingOptions.MessageTemplate = REQUEST_LOG_MESSAGE_TEMPLATE;

            requestLoggingOptions.GetLevel = (httpContext, elapsedMilliseconds, exception) =>
                exception is not null || httpContext.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode >= 400
                        ? LogEventLevel.Warning
                        : LogEventLevel.Information;

            requestLoggingOptions.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set(
                    CORRELATION_IDENTIFIER_PROPERTY,
                    httpContext.TraceIdentifier);

                diagnosticContext.Set(
                    USER_NAME_PROPERTY,
                    httpContext.User.Identity?.IsAuthenticated == true
                        ? httpContext.User.Identity.Name ?? string.Empty
                        : Security.HttpContextCurrentUserAccessor.ANONYMOUS_USER_NAME);

                diagnosticContext.Set(
                    CLIENT_ADDRESS_PROPERTY,
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
            };
        });

        return application;
    }
}
