using SB.API_SB.Domain.Common;

namespace SB.API_SB.Application.Common;

/// <summary>
/// Envoltura de respuesta paginada expuesta por la API. Se mantiene separada de
/// <see cref="PagedList{TItem}"/> para que un cambio en el contrato publico no
/// obligue a modificar el dominio.
/// </summary>
/// <typeparam name="TItem">Tipo de los elementos devueltos.</typeparam>
public sealed class PagedResponse<TItem>
{
    public PagedResponse(
        IReadOnlyCollection<TItem> items,
        int pageNumber,
        int pageSize,
        int totalCount,
        int totalPages)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = totalPages;
    }

    /// <summary>Elementos de la pagina.</summary>
    public IReadOnlyCollection<TItem> Items { get; }

    /// <summary>Numero de pagina devuelto.</summary>
    public int PageNumber { get; }

    /// <summary>Cantidad de elementos por pagina.</summary>
    public int PageSize { get; }

    /// <summary>Cantidad total de elementos que satisfacen el filtro.</summary>
    public int TotalCount { get; }

    /// <summary>Cantidad total de paginas.</summary>
    public int TotalPages { get; }

    /// <summary>Indica si existe una pagina anterior.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Indica si existe una pagina siguiente.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Proyecta una pagina de entidades de dominio a una pagina de respuestas.
    /// </summary>
    /// <typeparam name="TEntity">Tipo de la entidad de origen.</typeparam>
    /// <param name="source">Pagina de entidades de dominio.</param>
    /// <param name="projection">Funcion de proyeccion a aplicar a cada elemento.</param>
    /// <returns>Pagina lista para devolverse desde la API.</returns>
    public static PagedResponse<TItem> FromPagedList<TEntity>(
        PagedList<TEntity> source,
        Func<TEntity, TItem> projection)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(projection);

        List<TItem> projectedItems = source.Items.Select(projection).ToList();

        return new PagedResponse<TItem>(
            projectedItems,
            source.PageNumber,
            source.PageSize,
            source.TotalCount,
            source.TotalPages);
    }
}
