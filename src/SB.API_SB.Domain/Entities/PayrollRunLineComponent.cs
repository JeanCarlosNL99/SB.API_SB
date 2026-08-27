using SB.API_SB.Domain.Common;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Componente individual del calculo de una linea de nomina, por ejemplo
/// "Horas extras" o "Incentivo sobre salario base".
/// </summary>
public sealed class PayrollRunLineComponent : AuditableEntity
{
    /// <summary>Linea de nomina a la que pertenece el componente.</summary>
    public Guid PayrollRunLineId { get; set; }

    /// <summary>Linea de nomina a la que pertenece el componente.</summary>
    public PayrollRunLine? PayrollRunLine { get; set; }

    /// <summary>Orden en que se muestra el componente dentro del desglose.</summary>
    public int SortOrder { get; set; }

    /// <summary>Nombre del concepto calculado.</summary>
    public string Concept { get; set; } = string.Empty;

    /// <summary>Explicacion aritmetica del concepto.</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>Importe del concepto.</summary>
    public decimal Amount { get; set; }
}
