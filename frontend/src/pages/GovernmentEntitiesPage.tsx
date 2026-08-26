import { useCallback, useState } from 'react';
import { governmentEntitiesApi } from '@/api/endpoints';
import {
  EmptyState,
  ErrorMessage,
  LoadingIndicator,
  SuccessMessage,
} from '@/components/Feedback';
import { GovernmentEntityForm } from '@/components/GovernmentEntityForm';
import { EditIcon, TrashIcon } from '@/components/Icons';
import { ConfirmationDialog, Modal } from '@/components/Modal';
import { Pagination } from '@/components/Pagination';
import { useAuthentication } from '@/hooks/useAuthentication';
import { useAsyncData } from '@/hooks/useAsyncData';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { formatDate } from '@/utils/formatters';
import { buildClickableRowProps } from '@/utils/tableInteraction';
import type {
  GovernmentEntity,
  GovernmentEntityFilter,
  PagedResponse,
  RecordStatus,
  UpdateGovernmentEntityRequest,
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
 * Consulta del mantenimiento de entidades gubernamentales.
 *
 * Los filtros se aplican de forma automatica: los desplegables al seleccionar y
 * el campo de texto mientras se escribe. Para que escribir no genere una
 * peticion por tecla, el valor del texto se propaga a la consulta con un retardo
 * corto de inactividad.
 */
export function GovernmentEntitiesPage() {
  const { canWriteMaintenance } = useAuthentication();

  const [filter, setFilter] = useState<GovernmentEntityFilter>(INITIAL_FILTER);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [entityBeingEdited, setEntityBeingEdited] = useState<GovernmentEntity | null>(null);
  const [entityBeingDeleted, setEntityBeingDeleted] = useState<GovernmentEntity | null>(null);
  const [operationError, setOperationError] = useState<unknown>(null);
  const [isProcessing, setIsProcessing] = useState(false);

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

  /**
   * Abre el formulario de edicion. Se extrae a una funcion porque la accion se
   * dispara desde dos lugares: el boton de la fila y el clic sobre la fila.
   */
  function openEditor(entity: GovernmentEntity) {
    setOperationError(null);
    setSuccessMessage(null);
    setEntityBeingEdited(entity);
  }

  function openDeleteConfirmation(entity: GovernmentEntity) {
    setOperationError(null);
    setSuccessMessage(null);
    setEntityBeingDeleted(entity);
  }

  async function handleUpdate(values: UpdateGovernmentEntityRequest) {
    if (!entityBeingEdited) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      await governmentEntitiesApi.update(entityBeingEdited.id, values);
      setEntityBeingEdited(null);
      setSuccessMessage(`La entidad "${values.name}" se actualizo correctamente.`);
      await entitiesQuery.reload();
      await catalogsQuery.reload();
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsProcessing(false);
    }
  }

  async function handleDelete() {
    if (!entityBeingDeleted) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      await governmentEntitiesApi.remove(entityBeingDeleted.id);
      setSuccessMessage(`La entidad "${entityBeingDeleted.name}" se elimino correctamente.`);
      setEntityBeingDeleted(null);
      await entitiesQuery.reload();
    } catch (error) {
      setOperationError(error);
      setEntityBeingDeleted(null);
    } finally {
      setIsProcessing(false);
    }
  }

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

        <SuccessMessage message={successMessage} />
        <ErrorMessage error={operationError ?? entitiesQuery.error} />

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
                      {canWriteMaintenance && <th aria-label="Acciones" />}
                    </tr>
                  </thead>
                  <tbody>
                    {entitiesQuery.data.items.map((entity) => (
                      <tr
                        key={entity.id}
                        {...(canWriteMaintenance
                          ? buildClickableRowProps(
                              () => openEditor(entity),
                              `Editar ${entity.name}`,
                            )
                          : {})}
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
                        {canWriteMaintenance && (
                          <td>
                            <div className="table__actions">
                              <button
                                type="button"
                                className="button button--icon"
                                title="Editar"
                                aria-label={`Editar ${entity.name}`}
                                onClick={() => openEditor(entity)}
                              >
                                <EditIcon />
                              </button>
                              <button
                                type="button"
                                className="button button--icon"
                                title="Eliminar"
                                aria-label={`Eliminar ${entity.name}`}
                                onClick={() => openDeleteConfirmation(entity)}
                              >
                                <TrashIcon />
                              </button>
                            </div>
                          </td>
                        )}
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
        title="Editar entidad gubernamental"
        description="Los cambios se guardan en la base de datos de texto plano del proyecto."
        isOpen={entityBeingEdited !== null}
        onClose={() => setEntityBeingEdited(null)}
      >
        {entityBeingEdited && (
          <GovernmentEntityForm
            entity={entityBeingEdited}
            catalogs={
              catalogsQuery.data ?? { categories: [], sectors: [], stateBranches: [] }
            }
            isSubmitting={isProcessing}
            submitError={operationError}
            onSubmit={handleUpdate}
            onCancel={() => setEntityBeingEdited(null)}
          />
        )}
      </Modal>

      <ConfirmationDialog
        isOpen={entityBeingDeleted !== null}
        title="Eliminar entidad gubernamental"
        message={`Se eliminara "${entityBeingDeleted?.name ?? ''}" del mantenimiento. Esta accion no se puede deshacer.`}
        isProcessing={isProcessing}
        onConfirm={handleDelete}
        onCancel={() => setEntityBeingDeleted(null)}
      />
    </>
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
