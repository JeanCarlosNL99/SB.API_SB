using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Services.Employees;

/// <summary>Manejador del empleado por horas.</summary>
public sealed class HourlyEmployeeTypeHandler : EmployeeTypeHandlerBase<HourlyEmployee>
{
    private const string HOURLY_WAGE_FIELD_NAME = "sueldoPorHora";
    private const string HOURS_WORKED_FIELD_NAME = "horasTrabajadas";

    /// <inheritdoc />
    public override EmployeeType HandledType => EmployeeType.Hourly;

    /// <inheritdoc />
    public override string TypeDescription => "Empleado por horas";

    /// <inheritdoc />
    protected override HourlyEmployee CreateEmptyEmployee() => new();

    /// <inheritdoc />
    protected override void ApplyValues(HourlyEmployee employee, EmployeeRequestBase request)
    {
        employee.HourlyWage = RequireValue(request.HourlyWage, HOURLY_WAGE_FIELD_NAME);
        employee.HoursWorked = RequireValue(request.HoursWorked, HOURS_WORKED_FIELD_NAME);
    }

    /// <inheritdoc />
    protected override void ProjectValues(HourlyEmployee employee, EmployeeResponse response)
    {
        response.HourlyWage = employee.HourlyWage;
        response.HoursWorked = employee.HoursWorked;
    }
}
