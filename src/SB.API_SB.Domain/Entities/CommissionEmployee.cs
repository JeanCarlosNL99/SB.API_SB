using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Empleado por comision. Percibe un porcentaje de sus ventas brutas.
/// </summary>
/// <remarks>
/// No se declara <c>sealed</c> de forma intencional: el empleado asalariado por
/// comision es una especializacion de este tipo y reutiliza su calculo de
/// comision, evitando duplicar la formula.
/// </remarks>
public class CommissionEmployee : Employee
{
    /// <summary>Ventas brutas generadas por el empleado en la semana.</summary>
    public decimal GrossSales { get; set; }

    /// <summary>Tarifa de comision expresada como fraccion decimal (0.10 equivale a 10%).</summary>
    public decimal CommissionRate { get; set; }

    /// <inheritdoc />
    public override EmployeeType Type => EmployeeType.Commission;

    /// <summary>
    /// Pago semanal = ventasBrutas * tarifaComision.
    /// </summary>
    /// <inheritdoc />
    public override decimal CalculateWeeklyPayment() => RoundCurrency(CalculateCommission());

    /// <inheritdoc />
    public override PaymentBreakdown BuildPaymentBreakdown()
    {
        decimal totalAmount = CalculateWeeklyPayment();

        PaymentComponent[] components =
        {
            BuildCommissionComponent()
        };

        return new PaymentBreakdown(
            Formula: "pagoSemanal = ventasBrutas * tarifaComision",
            Components: components,
            TotalAmount: totalAmount);
    }

    /// <summary>Calcula la comision bruta sobre las ventas, sin redondear.</summary>
    /// <returns>Comision correspondiente a las ventas de la semana.</returns>
    protected decimal CalculateCommission() => GrossSales * CommissionRate;

    /// <summary>Construye el componente de reporte correspondiente a la comision.</summary>
    /// <returns>Componente con el detalle de la comision.</returns>
    protected PaymentComponent BuildCommissionComponent() => new(
        Concept: "Comision por ventas",
        Detail: $"{GrossSales:N2} de ventas x {CommissionRate:P2} de comision",
        Amount: RoundCurrency(CalculateCommission()));
}
