using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Application.Interfaces.Payroll;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Services.Payroll;

/// <summary>
/// Convierte una coleccion de empleados en las lineas de una nomina.
/// </summary>
/// <remarks>
/// Es la unica pieza que traduce del modelo de empleados al documento de nomina,
/// y la comparten la vista previa y la generacion. Que ambas usen exactamente el
/// mismo codigo es lo que garantiza que lo que el usuario revisa antes de generar
/// sea identico a lo que queda almacenado.
///
/// El calculo en si no ocurre aqui: cada empleado calcula su propio pago. Esta
/// clase solo recoge el resultado y lo copia a la instantanea.
/// </remarks>
public sealed class PayrollCalculator : IPayrollCalculator
{
    private const string UNASSIGNED_DEPARTMENT_NAME = "Sin departamento";

    private readonly IEmployeeTypeHandlerResolver typeHandlerResolver;

    public PayrollCalculator(IEmployeeTypeHandlerResolver typeHandlerResolver)
    {
        this.typeHandlerResolver = typeHandlerResolver;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<PayrollRunLine> BuildLines(
        IReadOnlyCollection<Employee> employees,
        PayrollWeek payrollWeek)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(payrollWeek);

        return employees
            .OrderBy(employee => employee.PaternalLastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(employee => employee.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .Select(BuildLine)
            .ToList();
    }

    private PayrollRunLine BuildLine(Employee employee)
    {
        IEmployeeTypeHandler typeHandler = typeHandlerResolver.Resolve(employee.Type);
        PaymentBreakdown breakdown = employee.BuildPaymentBreakdown();

        PayrollRunLine line = new()
        {
            EmployeeId = employee.Id,
            EmployeeFullName = employee.FullName,
            SocialSecurityNumber = employee.SocialSecurityNumber,
            EmployeeType = employee.Type,
            EmployeeTypeDescription = typeHandler.TypeDescription,
            DepartmentName = string.IsNullOrWhiteSpace(employee.Department?.Name)
                ? UNASSIGNED_DEPARTMENT_NAME
                : employee.Department!.Name,
            WeeklyPayment = breakdown.TotalAmount,
            PaymentFormula = breakdown.Formula
        };

        int sortOrder = 0;

        foreach (PaymentComponent component in breakdown.Components)
        {
            line.Components.Add(new PayrollRunLineComponent
            {
                SortOrder = sortOrder++,
                Concept = component.Concept,
                Detail = component.Detail,
                Amount = component.Amount
            });
        }

        return line;
    }
}
