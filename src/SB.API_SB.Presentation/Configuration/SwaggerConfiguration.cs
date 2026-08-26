using System.Reflection;
using Microsoft.OpenApi.Models;

namespace SB.API_SB.Presentation.Configuration;

/// <summary>
/// Configuracion de la documentacion interactiva con Swagger (OpenAPI).
/// </summary>
/// <remarks>
/// Ademas del listado de endpoints, se declara el esquema de seguridad Bearer
/// para poder autenticarse y probar la API desde la propia pagina de Swagger, y
/// se incluyen los comentarios XML del codigo como descripcion de cada operacion.
/// </remarks>
public static class SwaggerConfiguration
{
    private const string DOCUMENT_NAME = "v1";
    private const string DOCUMENT_TITLE = "SB.API_SB - API de Mantenimientos y Nomina";
    private const string DOCUMENT_VERSION = "1.0.0";
    private const string SECURITY_SCHEME_NAME = "Bearer";
    private const string DOCUMENT_DESCRIPTION =
        "API RESTful desarrollada en .NET 8 con arquitectura Onion. Incluye el " +
        "mantenimiento de entidades gubernamentales de la Republica Dominicana " +
        "(persistido en archivo de texto plano), la gestion de empleados con " +
        "calculo de nomina por tipo de contrato y la administracion de usuarios y " +
        "roles con autenticacion JWT.";

    private const string SECURITY_SCHEME_DESCRIPTION =
        "Autenticacion JWT. Obtenga el token en POST /api/autenticacion/iniciar-sesion " +
        "y escriba unicamente el valor del token (sin el prefijo Bearer).";

    /// <summary>Registra el generador de documentacion OpenAPI.</summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <returns>La coleccion de servicios, para permitir encadenamiento.</returns>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(swaggerOptions =>
        {
            swaggerOptions.SwaggerDoc(DOCUMENT_NAME, new OpenApiInfo
            {
                Title = DOCUMENT_TITLE,
                Version = DOCUMENT_VERSION,
                Description = DOCUMENT_DESCRIPTION,
                Contact = new OpenApiContact
                {
                    Name = "Superintendencia de Bancos de la Republica Dominicana"
                }
            });

            swaggerOptions.AddSecurityDefinition(SECURITY_SCHEME_NAME, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = SECURITY_SCHEME_DESCRIPTION
            });

            swaggerOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = SECURITY_SCHEME_NAME
                        }
                    },
                    Array.Empty<string>()
                }
            });

            swaggerOptions.EnableAnnotations();
            IncludeXmlComments(swaggerOptions);
        });

        return services;
    }

    /// <summary>Expone la interfaz de Swagger en la aplicacion.</summary>
    /// <param name="application">Aplicacion web en construccion.</param>
    /// <returns>La aplicacion, para permitir encadenamiento.</returns>
    public static WebApplication UseApiDocumentation(this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.UseSwagger();
        application.UseSwaggerUI(uiOptions =>
        {
            uiOptions.SwaggerEndpoint($"/swagger/{DOCUMENT_NAME}/swagger.json", DOCUMENT_TITLE);
            uiOptions.DocumentTitle = DOCUMENT_TITLE;
            uiOptions.DisplayRequestDuration();
        });

        return application;
    }

    private static void IncludeXmlComments(
        Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions swaggerOptions)
    {
        // Se incluyen los comentarios XML de la capa de Presentacion y de la de
        // Aplicacion, de modo que la documentacion describa tanto los endpoints
        // como los contratos que reciben y devuelven.
        string[] assemblyNames =
        {
            Assembly.GetExecutingAssembly().GetName().Name!,
            typeof(Application.Contracts.Employees.EmployeeResponse).Assembly.GetName().Name!
        };

        foreach (string assemblyName in assemblyNames)
        {
            string xmlFilePath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");

            if (File.Exists(xmlFilePath))
            {
                swaggerOptions.IncludeXmlComments(xmlFilePath, includeControllerXmlComments: true);
            }
        }
    }
}
