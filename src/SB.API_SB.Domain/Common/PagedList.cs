namespace SB.API_SB.Domain.Common;

/// <summary>
/// Resultado paginado de una consulta. Se define en el dominio porque la
/// paginacion es una necesidad transversal de las consultas de negocio y no un
/// detalle de la tecnologia de persistencia.
/// </summary>
/// <typeparam name="TItem">Tipo de los elementos contenidos en la pagina.</typeparam>
public sealed class PagedList<TItem>
{
    /// <summary>Numero de pagina por defecto cuando el cliente no especifica uno.</summary>
    public const int DEFAULT_PAGE_NUMBER = 1;

    /// <summary>Cantidad de elementos por pagina por defecto.</summary>
    public const int DEFAULT_PAGE_SIZE = 10;

    /// <summary>Cantidad maxima de elementos que se permiten solicitar por pagina.</summary>
    public const int MAXIMUM_PAGE_SIZE = 200;

    public PagedList(IReadOnlyCollection<TItem> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>Elementos correspondientes a la pagina solicitada.</summary>
    public IReadOnlyCollection<TItem> Items { get; }

    /// <summary>Cantidad total de elementos que satisfacen el filtro.</summary>
    public int TotalCount { get; }

    /// <summary>Numero de la pagina devuelta.</summary>
    public int PageNumber { get; }

    /// <summary>Cantidad de elementos solicitados por pagina.</summary>
    public int PageSize { get; }

    /// <summary>Cantidad total de paginas disponibles.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Indica si existe una pagina anterior.</summary>
    public bool HasPreviousPage => PageNumber > DEFAULT_PAGE_NUMBER;

    /// <summary>Indica si existe una pagina siguiente.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Crea una pagina vacia, util para consultas sin resultados.</summary>
    public static PagedList<TItem> Empty(int pageNumber, int pageSize) =>
        new(Array.Empty<TItem>(), totalCount: 0, pageNumber, pageSize);

    /// <summary>Normaliza los parametros de paginacion recibidos desde el exterior.</summary>
    public static (int PageNumber, int PageSize) NormalizePagination(int pageNumber, int pageSize)
    {
        int normalizedPageNumber = pageNumber < DEFAULT_PAGE_NUMBER ? DEFAULT_PAGE_NUMBER : pageNumber;
        int normalizedPageSize = pageSize switch
        {
            <= 0 => DEFAULT_PAGE_SIZE,
            > MAXIMUM_PAGE_SIZE => MAXIMUM_PAGE_SIZE,
            _ => pageSize
        };

        return (normalizedPageNumber, normalizedPageSize);
    }
}
