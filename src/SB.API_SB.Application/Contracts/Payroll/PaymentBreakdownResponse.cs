namespace SB.API_SB.Application.Contracts.Payroll;

/// <summary>Desglose del calculo de pago devuelto por la API.</summary>
public sealed class PaymentBreakdownResponse
{
    /// <summary>Formula aplicada, en formato legible.</summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>Componentes que suman el pago total.</summary>
    public IReadOnlyCollection<PaymentComponentResponse> Components { get; set; } =
        Array.Empty<PaymentComponentResponse>();

    /// <summary>Monto total del pago semanal.</summary>
    public decimal TotalAmount { get; set; }
}
