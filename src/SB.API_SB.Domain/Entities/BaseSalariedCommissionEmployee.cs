using SB.API_SB.Domain.Constants;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Empleado asalariado por comision. Ademas de la comision sobre ventas,
/// percibe un salario base con un incentivo del 10% sobre ese salario.
/// </summary>
public sealed class BaseSalariedCommissionEmployee : CommissionEmployee
{
    /// <summary>Salario base semanal garantizado al empleado.</summary>
    public decimal BaseSalary { get; set; }

    /// <inheritdoc />
    public override EmployeeType Type => EmployeeType.BaseSalariedCommission;

    /// <summary>
    /// Pago semanal = (ventasBrutas * tarifaComision) + salarioBase +
    /// (salarioBase * 0.10).
    /// </summary>
    /// <inheritdoc />
    public override decimal CalculateWeeklyPayment() =>
        RoundCurrency(CalculateCommission() + BaseSalary + CalculateBaseSalaryBonus());

    /// <inheritdoc />
    public override PaymentBreakdown BuildPaymentBreakdown()
    {
        PaymentComponent[] components =
        {
            BuildCommissionComponent(),
            new(
                Concept: "Salario base",
                Detail: $"Salario base semanal de {BaseSalary:N2}",
                Amount: RoundCurrency(BaseSalary)),
            new(
                Concept: "Incentivo sobre salario base",
                Detail: $"{BaseSalary:N2} x {PayrollConstants.BASE_SALARY_BONUS_PERCENTAGE:P0}",
                Amount: RoundCurrency(CalculateBaseSalaryBonus()))
        };

        return new PaymentBreakdown(
            Formula: "pagoSemanal = (ventasBrutas * tarifaComision) + salarioBase + (salarioBase * 0.10)",
            Components: components,
            TotalAmount: CalculateWeeklyPayment());
    }

    private decimal CalculateBaseSalaryBonus() =>
        BaseSalary * PayrollConstants.BASE_SALARY_BONUS_PERCENTAGE;
}
