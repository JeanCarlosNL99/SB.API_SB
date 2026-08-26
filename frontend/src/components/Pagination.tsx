import type { PagedResponse } from '@/types/api';

/** Opciones de tamano de pagina ofrecidas al usuario. */
const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];

/**
 * Controles de paginacion reutilizables.
 *
 * La paginacion se resuelve en el servidor: este componente solo informa la
 * pagina y el tamano solicitados, de modo que el navegador nunca descarga la
 * tabla completa.
 */
export function Pagination<TItem>({
  page,
  onPageChange,
  onPageSizeChange,
}: {
  page: PagedResponse<TItem>;
  onPageChange: (pageNumber: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}) {
  const firstItemNumber =
    page.totalCount === 0 ? 0 : (page.pageNumber - 1) * page.pageSize + 1;

  const lastItemNumber = Math.min(page.pageNumber * page.pageSize, page.totalCount);

  return (
    <div className="pagination">
      <div>
        Mostrando {firstItemNumber}-{lastItemNumber} de {page.totalCount} registro(s)
      </div>

      <div className="pagination__controls">
        <label className="field__hint" htmlFor="pageSize">
          Por pagina
        </label>
        <select
          id="pageSize"
          className="control"
          style={{ width: 'auto' }}
          value={page.pageSize}
          onChange={(changeEvent) => onPageSizeChange(Number(changeEvent.target.value))}
        >
          {PAGE_SIZE_OPTIONS.map((pageSizeOption) => (
            <option key={pageSizeOption} value={pageSizeOption}>
              {pageSizeOption}
            </option>
          ))}
        </select>

        <button
          type="button"
          className="button button--secondary"
          onClick={() => onPageChange(page.pageNumber - 1)}
          disabled={!page.hasPreviousPage}
        >
          Anterior
        </button>

        <span>
          Pagina {page.pageNumber} de {Math.max(page.totalPages, 1)}
        </span>

        <button
          type="button"
          className="button button--secondary"
          onClick={() => onPageChange(page.pageNumber + 1)}
          disabled={!page.hasNextPage}
        >
          Siguiente
        </button>
      </div>
    </div>
  );
}
