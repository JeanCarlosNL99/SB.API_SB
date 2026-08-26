using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Services.Employees;

/// <summary>Manejador del empleado asalariado.</summary>
public sealed class SalariedEmployeeTypeHandler : EmployeeTypeHandlerBase<SalariedEmployee>
{
    private const string WEEKLY_SALARY_FIELD_NAME = "salarioSemanal";

    /// <inheritdoc />
    public override EmployeeType HandledType => EmployeeType.Salaried;

    /// <inheritdoc />
    public override string TypeDescription => "Empleado asalariado";

    /// <inheritdoc />
    protected override SalariedEmployee CreateEmptyEmployee() => new();

    /// <inheritdoc />
    protected override void ApplyValues(SalariedEmployee employee, EmployeeRequestBase request)
    {
        employee.WeeklySalary = RequireValue(request.WeeklySalary, WEEKLY_SALARY_FIELD_NAME);
    }

    /// <inheritdoc />
    protected override void ProjectValues(SalariedEmployee employee, EmployeeResponse response)
    {
        response.WeeklySalary = employee.WeeklySalary;
    }
}
