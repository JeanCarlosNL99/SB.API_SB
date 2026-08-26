using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Application.Mappings;

/// <summary>
/// Proyecciones entre el objeto de valor de dominio del calculo de pago y su
/// contrato publico. Se escriben a mano en lugar de usar un mapeador automatico:
/// el mapeo queda explicito, sin reflexion en tiempo de ejecucion y sin
/// sorpresas cuando se renombra una propiedad.
/// </summary>
public static class PaymentBreakdownMappings
{
    /// <summary>Convierte el desglose de dominio en su contrato de respuesta.</summary>
    /// <param name="breakdown">Desglose calculado por el dominio.</param>
    /// <returns>Desglose listo para serializarse.</returns>
    public static PaymentBreakdownResponse ToResponse(this PaymentBreakdown breakdown)
    {
        ArgumentNullException.ThrowIfNull(breakdown);

        return new PaymentBreakdownResponse
        {
            Formula = breakdown.Formula,
            TotalAmount = breakdown.TotalAmount,
            Components = breakdown.Components
                .Select(component => new PaymentComponentResponse
                {
                    Concept = component.Concept,
                    Detail = component.Detail,
                    Amount = component.Amount
                })
                .ToList()
        };
    }
}
