using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Exceptions;

namespace SB.API_SB.Services.Employees;

/// <summary>
/// Base comun de los manejadores de tipo de empleado. Aporta la comprobacion
/// defensiva de los campos obligatorios del tipo y la conversion segura de la
/// entidad al subtipo esperado.
/// </summary>
/// <typeparam name="TEmployee">Subtipo de empleado atendido.</typeparam>
public abstract class EmployeeTypeHandlerBase<TEmployee> : IEmployeeTypeHandler
    where TEmployee : Employee
{
    /// <inheritdoc />
    public abstract EmployeeType HandledType { get; }

    /// <inheritdoc />
    public abstract string TypeDescription { get; }

    /// <inheritdoc />
    public Employee CreateEmployee(EmployeeRequestBase request)
    {
        ArgumentNullException.ThrowIfNull(request);

        TEmployee employee = CreateEmptyEmployee();

        ApplyValues(employee, request);

        return employee;
    }

    /// <inheritdoc />
    public void ApplyTypeSpecificValues(Employee employee, EmployeeRequestBase request)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(request);

        ApplyValues(EnsureExpectedType(employee), request);
    }

    /// <inheritdoc />
    public void ProjectTypeSpecificValues(Employee employee, EmployeeResponse response)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(response);

        ProjectValues(EnsureExpectedType(employee), response);
    }

    /// <summary>Crea una instancia vacia del subtipo atendido.</summary>
    /// <returns>Nueva instancia del subtipo.</returns>
    protected abstract TEmployee CreateEmptyEmployee();

    /// <summary>Copia en la entidad los valores propios del tipo.</summary>
    /// <param name="employee">Entidad destino.</param>
    /// <param name="request">Datos de origen.</param>
    protected abstract void ApplyValues(TEmployee employee, EmployeeRequestBase request);

    /// <summary>Copia hacia la respuesta los valores propios del tipo.</summary>
    /// <param name="employee">Entidad de origen.</param>
    /// <param name="response">Respuesta destino.</param>
    protected abstract void ProjectValues(TEmployee employee, EmployeeResponse response);

    /// <summary>
    /// Obtiene el valor de un campo obligatorio del tipo. Las validaciones ya
    /// deberian haberlo exigido; esta comprobacion protege ante llamadas internas
    /// que omitan la validacion.
    /// </summary>
    /// <param name="value">Valor recibido.</param>
    /// <param name="fieldName">Nombre del campo, para el mensaje de error.</param>
    /// <returns>El valor confirmado.</returns>
    protected decimal RequireValue(decimal? value, string fieldName)
    {
        if (!value.HasValue)
        {
            throw new BusinessRuleViolationException(
                $"El campo '{fieldName}' es obligatorio para el tipo de empleado {TypeDescription}.");
        }

        return value.Value;
    }

    private TEmployee EnsureExpectedType(Employee employee)
    {
        if (employee is not TEmployee typedEmployee)
        {
            throw new BusinessRuleViolationException(
                "No se puede cambiar el tipo de contrato de un empleado ya registrado. " +
                "Registre un nuevo empleado con el tipo deseado.");
        }

        return typedEmployee;
    }
}
