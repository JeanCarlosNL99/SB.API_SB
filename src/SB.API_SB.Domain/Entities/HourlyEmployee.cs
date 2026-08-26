using SB.API_SB.Domain.Constants;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Empleado por horas. Cobra por hora trabajada y recibe un recargo de 1.5
/// veces su sueldo por hora sobre las horas que exceden la jornada ordinaria.
/// </summary>
public sealed class HourlyEmployee : Employee
{
    /// <summary>Sueldo pactado por hora trabajada.</summary>
    public decimal HourlyWage { get; set; }

    /// <summary>Horas efectivamente trabajadas en la semana.</summary>
    public decimal HoursWorked { get; set; }

    /// <inheritdoc />
    public override EmployeeType Type => EmployeeType.Hourly;

    /// <summary>Horas trabajadas dentro de la jornada ordinaria.</summary>
    public decimal RegularHours => Math.Min(HoursWorked, PayrollConstants.STANDARD_WEEKLY_HOURS);

    /// <summary>Horas trabajadas por encima de la jornada ordinaria.</summary>
    public decimal OvertimeHours => Math.Max(
        HoursWorked - PayrollConstants.STANDARD_WEEKLY_HOURS,
        0m);

    /// <summary>
    /// Si horasTrabajadas es menor o igual a 40: pago = sueldoPorHora * horasTrabajadas.
    /// Si horasTrabajadas es mayor que 40: pago = (sueldoPorHora * 40) +
    /// (sueldoPorHora * 1.5 * (horasTrabajadas - 40)).
    /// </summary>
    /// <inheritdoc />
    public override decimal CalculateWeeklyPayment() =>
        RoundCurrency(CalculateRegularPayment() + CalculateOvertimePayment());

    /// <inheritdoc />
    public override PaymentBreakdown BuildPaymentBreakdown()
    {
        decimal regularPayment = RoundCurrency(CalculateRegularPayment());
        decimal overtimePayment = RoundCurrency(CalculateOvertimePayment());

        List<PaymentComponent> components = new()
        {
            new PaymentComponent(
                Concept: "Horas ordinarias",
                Detail: $"{RegularHours:N2} horas x {HourlyWage:N2}",
                Amount: regularPayment)
        };

        if (OvertimeHours > 0m)
        {
            components.Add(new PaymentComponent(
                Concept: "Horas extras",
                Detail: $"{OvertimeHours:N2} horas x {HourlyWage:N2} x {PayrollConstants.OVERTIME_RATE_MULTIPLIER:N1}",
                Amount: overtimePayment));
        }

        string formula = OvertimeHours > 0m
            ? "pagoSemanal = (sueldoPorHora * 40) + (sueldoPorHora * 1.5 * (horasTrabajadas - 40))"
            : "pagoSemanal = sueldoPorHora * horasTrabajadas";

        return new PaymentBreakdown(
            Formula: formula,
            Components: components,
            TotalAmount: CalculateWeeklyPayment());
    }

    private decimal CalculateRegularPayment() => HourlyWage * RegularHours;

    private decimal CalculateOvertimePayment() =>
        HourlyWage * PayrollConstants.OVERTIME_RATE_MULTIPLIER * OvertimeHours;
}
