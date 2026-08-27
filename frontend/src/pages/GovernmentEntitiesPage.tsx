import { useCallback, useState } from 'react';
import { governmentEntitiesApi } from '@/api/endpoints';
import {
  EmptyState,
  ErrorMessage,
  LoadingIndicator,
} from '@/components/Feedback';
import { DetailIcon } from '@/components/Icons';
import { Modal } from '@/components/Modal';
import { Pagination } from '@/components/Pagination';
import { useAsyncData } from '@/hooks/useAsyncData';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { formatDate } from '@/utils/formatters';
import { buildClickableRowProps } from '@/utils/tableInteraction';
import type {
  GovernmentEntity,
  GovernmentEntityFilter,
  PagedResponse,
  RecordStatus,
} from '@/types/api';

const INITIAL_FILTER: GovernmentEntityFilter = {
  name: '',
  category: '',
  sector: '',
  stateBranch: '',
  status: '',
  pageNumber: 1,
  pageSize: 10,
};

/**
 * Consulta del listado oficial de entidades gubernamentales.
 *
 * El listado es un catalogo de solo lectura: se distribuye con la aplicacion en
 * el archivo de texto plano y es la fuente a la que se asocia cada empleado. La
 * pantalla consulta y muestra el detalle; no lo administra.
 *
 * Los filtros se aplican de forma automatica: los desplegables al seleccionar y
 * el campo de texto mientras se escribe. Para que escribir no genere una
 * peticion por tecla, el valor del texto se propaga a la consulta con un retardo
 * corto de inactividad.
 */
export function GovernmentEntitiesPage() {
  const [filter, setFilter] = useState<GovernmentEntityFilter>(INITIAL_FILTER);
  const [entityBeingViewed, setEntityBeingViewed] = useState<GovernmentEntity | null>(
    null,
  );

  const catalogsQuery = useAsyncData(() => governmentEntitiesApi.getCatalogs(), []);

  // El campo de texto responde de inmediato en pantalla, pero la consulta se
  // lanza cuando el usuario deja de escribir.
  const debouncedName = useDebouncedValue(filter.name ?? '');

  const entitiesQuery = useAsyncData<PagedResponse<GovernmentEntity>>(
    () => governmentEntitiesApi.search({ ...filter, name: debouncedName }),
    [
      debouncedName,
      filter.category,
      filter.sector,
      filter.stateBranch,
      filter.status,
      filter.pageNumber,
      filter.pageSize,
    ],
  );

  /**
   * Aplica un cambio de filtro y vuelve a la primera pagina: mantener la pagina
   * anterior mostraria un resultado vacio cuando el nuevo filtro devuelve menos
   * paginas que la actual.
   */
  const updateFilter = useCallback((changes: Partial<GovernmentEntityFilter>) => {
    setFilter((previousFilter) => ({ ...previousFilter, ...changes, pageNumber: 1 }));
  }, []);

  const clearFilters = useCallback(() => {
    setFilter(INITIAL_FILTER);
  }, []);

  const hasActiveFilters =
    (filter.name ?? '') !== '' ||
    (filter.category ?? '') !== '' ||
    (filter.sector ?? '') !== '' ||
    (filter.stateBranch ?? '') !== '' ||
    (filter.status ?? '') !== '';

  // Ya hay datos en pantalla y se esta trayendo un resultado nuevo: se atenua la
  // tabla en lugar de sustituirla, para no parpadear en cada pulsacion.
  const isRefreshing = entitiesQuery.isLoading && entitiesQuery.data !== null;
  const isFirstLoad = entitiesQuery.isLoading && entitiesQuery.data === null;

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Filtros de consulta</h2>
            <p className="card__description">
              Los filtros se combinan entre si, se aplican al instante y se resuelven en
              el servidor.
            </p>
          </div>
          <button
            type="button"
            className="button button--secondary"
            onClick={clearFilters}
            disabled={!hasActiveFilters}
          >
            Limpiar filtros
          </button>
        </div>

        <div className="filters">
          <div className="field">
            <label className="field__label" htmlFor="filterName">
              Nombre
            </label>
            <input
              id="filterName"
              className="control"
              type="search"
              placeholder="Escriba para filtrar"
              autoComplete="off"
              value={filter.name ?? ''}
              onChange={(changeEvent) => updateFilter({ name: changeEvent.target.value })}
            />
          </div>

          <SelectFilter
            id="filterCategory"
            label="Categoria"
            options={catalogsQuery.data?.categories ?? []}
            value={filter.category ?? ''}
            onChange={(value) => updateFilter({ category: value })}
          />

          <SelectFilter
            id="filterSector"
            label="Sector"
            options={catalogsQuery.data?.sectors ?? []}
            value={filter.sector ?? ''}
            onChange={(value) => updateFilter({ sector: value })}
          />

          <SelectFilter
            id="filterStateBranch"
            label="Poder del Estado"
            options={catalogsQuery.data?.stateBranches ?? []}
            value={filter.stateBranch ?? ''}
            onChange={(value) => updateFilter({ stateBranch: value })}
          />

          <div className="field">
            <label className="field__label" htmlFor="filterStatus">
              Estado
            </label>
            <select
              id="filterStatus"
              className="control"
              value={filter.status ?? ''}
              onChange={(changeEvent) =>
                updateFilter({ status: changeEvent.target.value as RecordStatus | '' })
              }
            >
              <option value="">Todos</option>
              <option value="Active">Activo</option>
              <option value="Inactive">Inactivo</option>
            </select>
          </div>
        </div>

        <div className="filters__status" aria-live="polite">
          {isRefreshing && (
            <>
              <span className="spinner" />
              Filtrando...
            </>
          )}
          {!entitiesQuery.isLoading && entitiesQuery.data && (
            <>
              {entitiesQuery.data.totalCount} entidad(es) coinciden con el filtro actual.
            </>
          )}
        </div>
      </section>

      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Resultados</h2>
            <p className="card__description">
              Listado oficial de entidades gubernamentales de la Republica Dominicana.
            </p>
          </div>
        </div>

        <ErrorMessage error={entitiesQuery.error} />

        {isFirstLoad && <LoadingIndicator />}

        {entitiesQuery.data && (
          <div className={isRefreshing ? 'is-refreshing' : undefined}>
            {entitiesQuery.data.items.length === 0 ? (
              <EmptyState
                title="No se encontraron entidades"
                description="Ajuste los filtros de la consulta e intente de nuevo."
              />
            ) : (
              <div className="table-wrapper">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Nombre</th>
                      <th>Categoria</th>
                      <th>Poder del Estado</th>
                      <th>Sector</th>
                      <th>Estado</th>
                      <th>Registrado</th>
                      <th aria-label="Acciones" />
                    </tr>
                  </thead>
                  <tbody>
                    {entitiesQuery.data.items.map((entity) => (
                      <tr
                        key={entity.id}
                        {...buildClickableRowProps(
                          () => setEntityBeingViewed(entity),
                          `Ver el detalle de ${entity.name}`,
                        )}
                      >
                        <td className="table td--wrap">{entity.name}</td>
                        <td>{entity.category}</td>
                        <td>{entity.stateBranch}</td>
                        <td>{entity.sector}</td>
                        <td>
                          <span
                            className={
                              entity.status === 'Active'
                                ? 'badge badge--active'
                                : 'badge badge--inactive'
                            }
                          >
                            {entity.statusDescription}
                          </span>
                        </td>
                        <td>{formatDate(entity.createdAt)}</td>
                        <td>
                          <div className="table__actions">
                            <button
                              type="button"
                              className="button button--icon"
                              title="Ver detalle"
                              aria-label={`Ver el detalle de ${entity.name}`}
                              onClick={() => setEntityBeingViewed(entity)}
                            >
                              <DetailIcon />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <Pagination
              page={entitiesQuery.data}
              onPageChange={(pageNumber) =>
                setFilter((previousFilter) => ({ ...previousFilter, pageNumber }))
              }
              onPageSizeChange={(pageSize) => updateFilter({ pageSize })}
            />
          </div>
        )}
      </section>

      <Modal
        title="Detalle de la entidad gubernamental"
        description="Registro del listado oficial, almacenado en la base de datos de texto plano del proyecto."
        isOpen={entityBeingViewed !== null}
        onClose={() => setEntityBeingViewed(null)}
      >
        {entityBeingViewed && (
          <div>
            <DetailRow label="Nombre" value={entityBeingViewed.name} />
            <DetailRow label="Categoria" value={entityBeingViewed.category} />
            <DetailRow label="Poder del Estado" value={entityBeingViewed.stateBranch} />
            <DetailRow label="Sector" value={entityBeingViewed.sector} />
            <DetailRow label="Estado" value={entityBeingViewed.statusDescription} />
            <DetailRow label="Registrado" value={formatDate(entityBeingViewed.createdAt)} />
          </div>
        )}
      </Modal>
    </>
  );
}

/** Fila de un par etiqueta/valor dentro del detalle. */
function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="detail-row">
      <span className="detail-row__label">{label}</span>
      <span className="detail-row__value">{value}</span>
    </div>
  );
}

function SelectFilter({
  id,
  label,
  options,
  value,
  onChange,
}: {
  id: string;
  label: string;
  options: string[];
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="field">
      <label className="field__label" htmlFor={id}>
        {label}
      </label>
      <select
        id={id}
        className="control"
        value={value}
        onChange={(changeEvent) => onChange(changeEvent.target.value)}
      >
        <option value="">Todos</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    </div>
  );
}
