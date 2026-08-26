namespace SB.API_SB.Presentation.Configuration;

/// <summary>
/// Configuracion de CORS para permitir que la aplicacion React consuma la API.
/// </summary>
/// <remarks>
/// Los origenes permitidos se leen de AppSettings.json en lugar de usar
/// <c>AllowAnyOrigin</c>: una politica abierta convertiria la API en consumible
/// desde cualquier sitio web.
/// </remarks>
public static class CorsConfiguration
{
    /// <summary>Nombre de la politica de CORS de la aplicacion cliente.</summary>
    public const string CLIENT_APPLICATION_POLICY_NAME = "AplicacionCliente";

    /// <summary>Nombre de la seccion de configuracion con los origenes permitidos.</summary>
    public const string ALLOWED_ORIGINS_SECTION_NAME = "Cors:AllowedOrigins";

    /// <summary>Registra la politica de CORS de la aplicacion cliente.</summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <param name="configuration">Configuracion de la aplicacion.</param>
    /// <returns>La coleccion de servicios, para permitir encadenamiento.</returns>
    public static IServiceCollection AddClientApplicationCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string[] allowedOrigins = configuration
            .GetSection(ALLOWED_ORIGINS_SECTION_NAME)
            .Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy(CLIENT_APPLICATION_POLICY_NAME, policyBuilder =>
            {
                if (allowedOrigins.Length == 0)
                {
                    return;
                }

                policyBuilder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Content-Disposition");
            });
        });

        return services;
    }
}
