using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Exceptions;

namespace SB.API_SB.Services.Employees;

/// <summary>
/// Registro de manejadores de tipo de empleado poblado por inyeccion de
/// dependencias.
/// </summary>
/// <remarks>
/// El contenedor inyecta todos los manejadores registrados y esta clase los
/// indexa por tipo. Al agregar un nuevo tipo de empleado solo hay que registrar
/// su manejador: ni el resolutor ni los servicios que lo consumen cambian.
/// </remarks>
public sealed class EmployeeTypeHandlerResolver : IEmployeeTypeHandlerResolver
{
    private readonly IReadOnlyDictionary<EmployeeType, IEmployeeTypeHandler> handlersByType;

    public EmployeeTypeHandlerResolver(IEnumerable<IEmployeeTypeHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        handlersByType = handlers.ToDictionary(handler => handler.HandledType);
    }

    /// <inheritdoc />
    public IEmployeeTypeHandler Resolve(EmployeeType employeeType)
    {
        if (!handlersByType.TryGetValue(employeeType, out IEmployeeTypeHandler? handler))
        {
            throw new BusinessRuleViolationException(
                $"No existe un manejador registrado para el tipo de empleado '{employeeType}'.");
        }

        return handler;
    }
}
