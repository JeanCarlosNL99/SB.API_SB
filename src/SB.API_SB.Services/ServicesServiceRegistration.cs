using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Application.Validators.Employees;
using SB.API_SB.Services.Employees;
using SB.API_SB.Services.Payroll;

namespace SB.API_SB.Services;

/// <summary>Registro de las dependencias de la capa de Servicios.</summary>
public static class ServicesServiceRegistration
{
    /// <summary>
    /// Agrega al contenedor los servicios de aplicacion, los manejadores de tipo
    /// de empleado y los validadores.
    /// </summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <returns>La coleccion de servicios, para permitir encadenamiento.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IGovernmentEntityService, GovernmentEntityService>();
        services.AddScoped<IPayrollReportService, PayrollReportService>();

        services.AddEmployeeTypeHandlers();
        services.AddRequestValidators();

        return services;
    }

    /// <summary>
    /// Registra un manejador por cada tipo de empleado soportado.
    /// </summary>
    /// <remarks>
    /// Este es el unico punto que hay que tocar para incorporar un nuevo tipo de
    /// contrato: se agrega la subclase en el dominio, su manejador y una linea
    /// aqui. Ningun servicio existente cambia.
    /// </remarks>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    private static void AddEmployeeTypeHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IEmployeeTypeHandler, SalariedEmployeeTypeHandler>();
        services.AddSingleton<IEmployeeTypeHandler, HourlyEmployeeTypeHandler>();
        services.AddSingleton<IEmployeeTypeHandler, CommissionEmployeeTypeHandler>();
        services.AddSingleton<IEmployeeTypeHandler, BaseSalariedCommissionEmployeeTypeHandler>();

        services.AddSingleton<IEmployeeTypeHandlerResolver, EmployeeTypeHandlerResolver>();
    }

    /// <summary>
    /// Registra todos los validadores declarados en el ensamblado de la capa de
    /// Aplicacion, descubriendolos por reflexion.
    /// </summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    private static void AddRequestValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateEmployeeRequestValidator>(
            ServiceLifetime.Scoped);
    }
}
