namespace SB.API_SB.Application.Contracts.Payroll;

/// <summary>Componente individual del calculo de pago.</summary>
public sealed class PaymentComponentResponse
{
    /// <summary>Nombre del concepto calculado.</summary>
    public string Concept { get; set; } = string.Empty;

    /// <summary>Explicacion aritmetica del concepto.</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>Importe del concepto.</summary>
    public decimal Amount { get; set; }
}
