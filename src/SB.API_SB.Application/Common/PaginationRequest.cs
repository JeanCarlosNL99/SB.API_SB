namespace SB.API_SB.Application.Common;

/// <summary>Parametros de paginacion comunes a todas las consultas de la API.</summary>
public abstract class PaginationRequest
{
    /// <summary>Numero de pagina solicitado. Inicia en 1.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Cantidad de registros por pagina.</summary>
    public int PageSize { get; set; } = 10;
}
