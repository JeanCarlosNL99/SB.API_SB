using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Domain.Interfaces.Criteria;

/// <summary>Criterios de busqueda del mantenimiento de entidades gubernamentales.</summary>
public sealed class GovernmentEntityFilterCriteria
{
    /// <summary>Texto a buscar en el nombre de la entidad.</summary>
    public string? Name { get; init; }

    /// <summary>Categoria administrativa a filtrar.</summary>
    public string? Category { get; init; }

    /// <summary>Sector a filtrar.</summary>
    public string? Sector { get; init; }

    /// <summary>Poder del Estado a filtrar.</summary>
    public string? StateBranch { get; init; }

    /// <summary>Estado del registro a filtrar.</summary>
    public RecordStatus? Status { get; init; }

    /// <summary>Numero de pagina solicitado.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Cantidad de registros por pagina.</summary>
    public int PageSize { get; init; } = 10;
}
