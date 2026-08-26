using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Services.Employees;

/// <summary>Manejador del empleado por comision.</summary>
public sealed class CommissionEmployeeTypeHandler : EmployeeTypeHandlerBase<CommissionEmployee>
{
    /// <summary>Nombre del campo de ventas brutas, compartido con el tipo asalariado por comision.</summary>
    internal const string GROSS_SALES_FIELD_NAME = "ventasBrutas";

    /// <summary>Nombre del campo de tarifa de comision, compartido con el tipo asalariado por comision.</summary>
    internal const string COMMISSION_RATE_FIELD_NAME = "tarifaComision";

    /// <inheritdoc />
    public override EmployeeType HandledType => EmployeeType.Commission;

    /// <inheritdoc />
    public override string TypeDescription => "Empleado por comision";

    /// <inheritdoc />
    protected override CommissionEmployee CreateEmptyEmployee() => new();

    /// <inheritdoc />
    protected override void ApplyValues(CommissionEmployee employee, EmployeeRequestBase request)
    {
        employee.GrossSales = RequireValue(request.GrossSales, GROSS_SALES_FIELD_NAME);
        employee.CommissionRate = RequireValue(request.CommissionRate, COMMISSION_RATE_FIELD_NAME);
    }

    /// <inheritdoc />
    protected override void ProjectValues(CommissionEmployee employee, EmployeeResponse response)
    {
        response.GrossSales = employee.GrossSales;
        response.CommissionRate = employee.CommissionRate;
    }
}
