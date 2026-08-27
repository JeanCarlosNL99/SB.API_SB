namespace SB.API_SB.Domain.Interfaces.Criteria;

/// <summary>Criterios de busqueda del historial de ejecuciones de nomina.</summary>
public sealed class PayrollRunFilterCriteria
{
    /// <summary>Entidad gubernamental por la que se desea filtrar.</summary>
    public Guid? GovernmentEntityId { get; init; }

    /// <summary>Ano por el que se desea filtrar.</summary>
    public int? Year { get; init; }

    /// <summary>Indica si se incluyen las ejecuciones anuladas.</summary>
    public bool IncludeCancelled { get; init; } = true;

    /// <summary>Numero de pagina solicitado.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Cantidad de registros por pagina.</summary>
    public int PageSize { get; init; } = 10;
}
