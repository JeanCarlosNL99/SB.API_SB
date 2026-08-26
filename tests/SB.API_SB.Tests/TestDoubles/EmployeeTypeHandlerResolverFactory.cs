using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Services.Employees;

namespace SB.API_SB.Tests.TestDoubles;

/// <summary>
/// Construye el resolutor de manejadores con los cuatro tipos reales, replicando
/// lo que hace el contenedor de inyeccion de dependencias en produccion.
/// </summary>
public static class EmployeeTypeHandlerResolverFactory
{
    /// <summary>Crea un resolutor con todos los manejadores registrados.</summary>
    /// <returns>Resolutor listo para usarse en pruebas.</returns>
    public static IEmployeeTypeHandlerResolver Create()
    {
        IEmployeeTypeHandler[] handlers =
        {
            new SalariedEmployeeTypeHandler(),
            new HourlyEmployeeTypeHandler(),
            new CommissionEmployeeTypeHandler(),
            new BaseSalariedCommissionEmployeeTypeHandler()
        };

        return new EmployeeTypeHandlerResolver(handlers);
    }
}
