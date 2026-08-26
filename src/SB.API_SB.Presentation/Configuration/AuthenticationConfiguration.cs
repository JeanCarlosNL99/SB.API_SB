using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SB.API_SB.Domain.Constants;
using SB.API_SB.Infrastructure.Options;
using SB.API_SB.Presentation.Authorization;

namespace SB.API_SB.Presentation.Configuration;

/// <summary>
/// Configuracion de la autenticacion por token Bearer (JWT) y de las politicas de
/// autorizacion por rol.
/// </summary>
public static class AuthenticationConfiguration
{
    /// <summary>Registra la validacion del token y las politicas de autorizacion.</summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <param name="configuration">Configuracion de la aplicacion.</param>
    /// <returns>La coleccion de servicios, para permitir encadenamiento.</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        JwtOptions jwtOptions = configuration
            .GetSection(JwtOptions.SECTION_NAME)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                $"La seccion '{JwtOptions.SECTION_NAME}' no esta definida en AppSettings.json.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearerOptions =>
            {
                bearerOptions.RequireHttpsMetadata = false;
                bearerOptions.SaveToken = true;

                // Todas las validaciones se activan explicitamente: emisor,
                // audiencia, vigencia y firma. Dejar alguna en falso convertiria el
                // token en un dato de entrada no confiable.
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
                };

                bearerOptions.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = failedContext =>
                    {
                        ILogger<JwtBearerEvents> logger = failedContext.HttpContext
                            .RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();

                        logger.LogWarning(
                            "Fallo la validacion del token en {Path}. Motivo: {Message}.",
                            failedContext.Request.Path,
                            failedContext.Exception.Message);

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationPolicies();

        return services;
    }

    private static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.AddPolicy(
                AuthorizationPolicies.ADMINISTRATION_ONLY,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(RoleNames.ADMINISTRATOR));

            authorizationOptions.AddPolicy(
                AuthorizationPolicies.MAINTENANCE_WRITE,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(RoleNames.ADMINISTRATOR, RoleNames.HUMAN_RESOURCES));

            authorizationOptions.AddPolicy(
                AuthorizationPolicies.MAINTENANCE_READ,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(
                        RoleNames.ADMINISTRATOR,
                        RoleNames.HUMAN_RESOURCES,
                        RoleNames.CONSULTANT));

            // Por omision, ningun endpoint queda abierto: se exige usuario
            // autenticado salvo que se marque explicitamente con AllowAnonymous.
            authorizationOptions.FallbackPolicy = authorizationOptions
                .GetPolicy(AuthorizationPolicies.MAINTENANCE_READ);
        });
    }
}
