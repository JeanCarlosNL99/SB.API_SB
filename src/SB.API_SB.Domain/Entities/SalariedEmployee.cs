using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Empleado asalariado. Percibe un salario semanal fijo, independiente de las
/// horas trabajadas o de las ventas realizadas.
/// </summary>
public sealed class SalariedEmployee : Employee
{
    /// <summary>Salario semanal fijo del empleado.</summary>
    public decimal WeeklySalary { get; set; }

    /// <inheritdoc />
    public override EmployeeType Type => EmployeeType.Salaried;

    /// <summary>
    /// Pago semanal = salarioSemanal.
    /// </summary>
    /// <inheritdoc />
    public override decimal CalculateWeeklyPayment() => RoundCurrency(WeeklySalary);

    /// <inheritdoc />
    public override PaymentBreakdown BuildPaymentBreakdown()
    {
        decimal totalAmount = CalculateWeeklyPayment();

        PaymentComponent[] components =
        {
            new(
                Concept: "Salario semanal",
                Detail: $"Salario fijo semanal de {WeeklySalary:N2}",
                Amount: totalAmount)
        };

        return new PaymentBreakdown(
            Formula: "pagoSemanal = salarioSemanal",
            Components: components,
            TotalAmount: totalAmount);
    }
}
