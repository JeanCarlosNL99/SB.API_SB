using SB.API_SB.Application.Common;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Contracts.GovernmentEntities;

/// <summary>Filtros aceptados por la consulta de entidades gubernamentales.</summary>
public sealed class GovernmentEntityFilterRequest : PaginationRequest
{
    /// <summary>Texto a buscar en el nombre de la entidad.</summary>
    public string? Name { get; set; }

    /// <summary>Categoria administrativa a filtrar.</summary>
    public string? Category { get; set; }

    /// <summary>Sector a filtrar.</summary>
    public string? Sector { get; set; }

    /// <summary>Poder del Estado a filtrar.</summary>
    public string? StateBranch { get; set; }

    /// <summary>Estado del registro a filtrar.</summary>
    public RecordStatus? Status { get; set; }
}
