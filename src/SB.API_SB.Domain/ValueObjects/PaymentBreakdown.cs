namespace SB.API_SB.Domain.ValueObjects;

/// <summary>
/// Detalle del calculo de pago de un empleado. Es un objeto de valor inmutable:
/// se construye a partir de la entidad y describe COMO se llego al monto, lo
/// que permite generar el reporte semanal "detallando los calculos" sin
/// duplicar la logica en la capa de presentacion.
/// </summary>
/// <param name="Formula">Expresion legible de la formula aplicada.</param>
/// <param name="Components">Componentes individuales que suman el pago total.</param>
/// <param name="TotalAmount">Monto total del pago semanal.</param>
public sealed record PaymentBreakdown(
    string Formula,
    IReadOnlyCollection<PaymentComponent> Components,
    decimal TotalAmount);

/// <summary>
/// Componente individual del calculo de pago (por ejemplo, "Horas ordinarias").
/// </summary>
/// <param name="Concept">Nombre del concepto calculado.</param>
/// <param name="Detail">Explicacion aritmetica del concepto.</param>
/// <param name="Amount">Importe del concepto.</param>
public sealed record PaymentComponent(string Concept, string Detail, decimal Amount);
