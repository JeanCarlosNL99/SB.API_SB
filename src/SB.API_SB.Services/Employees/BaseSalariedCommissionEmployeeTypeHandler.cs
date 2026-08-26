using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Services.Employees;

/// <summary>
/// Manejador del empleado asalariado por comision. Reutiliza los nombres de campo
/// del tipo por comision porque comparte esos datos de captura.
/// </summary>
public sealed class BaseSalariedCommissionEmployeeTypeHandler
    : EmployeeTypeHandlerBase<BaseSalariedCommissionEmployee>
{
    private const string BASE_SALARY_FIELD_NAME = "salarioBase";

    /// <inheritdoc />
    public override EmployeeType HandledType => EmployeeType.BaseSalariedCommission;

    /// <inheritdoc />
    public override string TypeDescription => "Empleado asalariado por comision";

    /// <inheritdoc />
    protected override BaseSalariedCommissionEmployee CreateEmptyEmployee() => new();

    /// <inheritdoc />
    protected override void ApplyValues(
        BaseSalariedCommissionEmployee employee,
        EmployeeRequestBase request)
    {
        employee.GrossSales = RequireValue(
            request.GrossSales,
            CommissionEmployeeTypeHandler.GROSS_SALES_FIELD_NAME);

        employee.CommissionRate = RequireValue(
            request.CommissionRate,
            CommissionEmployeeTypeHandler.COMMISSION_RATE_FIELD_NAME);

        employee.BaseSalary = RequireValue(request.BaseSalary, BASE_SALARY_FIELD_NAME);
    }

    /// <inheritdoc />
    protected override void ProjectValues(
        BaseSalariedCommissionEmployee employee,
        EmployeeResponse response)
    {
        response.GrossSales = employee.GrossSales;
        response.CommissionRate = employee.CommissionRate;
        response.BaseSalary = employee.BaseSalary;
    }
}
