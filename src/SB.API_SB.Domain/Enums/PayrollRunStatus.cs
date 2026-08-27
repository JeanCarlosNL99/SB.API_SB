namespace SB.API_SB.Domain.Enums;

/// <summary>Estado de una ejecucion de nomina.</summary>
public enum PayrollRunStatus
{
    /// <summary>La nomina de la semana fue calculada y queda como historico.</summary>
    Generated = 1,

    /// <summary>
    /// La ejecucion fue anulada por un administrador. Se conserva como evidencia,
    /// pero libera la semana para volver a calcularla.
    /// </summary>
    Cancelled = 2
}
