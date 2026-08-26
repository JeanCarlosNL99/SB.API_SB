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
import { formatDate } from '@/utils/formatters';
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
 * Los filtros se aplican al presionar "Buscar" y no en cada pulsacion de tecla:
 * una consulta por letra escrita generaria peticiones que el usuario nunca llega
 * a leer. El estado del formulario y el de la consulta se mantienen separados
 * para lograrlo.
 */
export function GovernmentEntitiesPage() {
  const { canWriteMaintenance } = useAuthentication();

  const [formFilter, setFormFilter] = useState<GovernmentEntityFilter>(INITIAL_FILTER);
  const [appliedFilter, setAppliedFilter] = useState<GovernmentEntityFilter>(INITIAL_FILTER);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [entityBeingEdited, setEntityBeingEdited] = useState<GovernmentEntity | null>(null);
  const [entityBeingDeleted, setEntityBeingDeleted] = useState<GovernmentEntity | null>(null);
  const [operationError, setOperationError] = useState<unknown>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  const catalogsQuery = useAsyncData(() => governmentEntitiesApi.getCatalogs(), []);

  const entitiesQuery = useAsyncData<PagedResponse<GovernmentEntity>>(
    () => governmentEntitiesApi.search(appliedFilter),
    [
      appliedFilter.name,
      appliedFilter.category,
      appliedFilter.sector,
      appliedFilter.stateBranch,
      appliedFilter.status,
      appliedFilter.pageNumber,
      appliedFilter.pageSize,
    ],
  );

  const applyFilters = useCallback(() => {
    setAppliedFilter({ ...formFilter, pageNumber: 1 });
  }, [formFilter]);

  const clearFilters = useCallback(() => {
    setFormFilter(INITIAL_FILTER);
    setAppliedFilter(INITIAL_FILTER);
  }, []);

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
              Los filtros se combinan entre si y se aplican en el servidor.
            </p>
          </div>
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
              placeholder="Ejemplo: Banco"
              value={formFilter.name ?? ''}
              onChange={(changeEvent) =>
                setFormFilter({ ...formFilter, name: changeEvent.target.value })
              }
              onKeyDown={(keyboardEvent) => {
                if (keyboardEvent.key === 'Enter') {
                  applyFilters();
                }
              }}
            />
          </div>

          <SelectFilter
            id="filterCategory"
            label="Categoria"
            options={catalogsQuery.data?.categories ?? []}
            value={formFilter.category ?? ''}
            onChange={(value) => setFormFilter({ ...formFilter, category: value })}
          />

          <SelectFilter
            id="filterSector"
            label="Sector"
            options={catalogsQuery.data?.sectors ?? []}
            value={formFilter.sector ?? ''}
            onChange={(value) => setFormFilter({ ...formFilter, sector: value })}
          />

          <SelectFilter
            id="filterStateBranch"
            label="Poder del Estado"
            options={catalogsQuery.data?.stateBranches ?? []}
            value={formFilter.stateBranch ?? ''}
            onChange={(value) => setFormFilter({ ...formFilter, stateBranch: value })}
          />

          <div className="field">
            <label className="field__label" htmlFor="filterStatus">
              Estado
            </label>
            <select
              id="filterStatus"
              className="control"
              value={formFilter.status ?? ''}
              onChange={(changeEvent) =>
                setFormFilter({
                  ...formFilter,
                  status: changeEvent.target.value as RecordStatus | '',
                })
              }
            >
              <option value="">Todos</option>
              <option value="Active">Activo</option>
              <option value="Inactive">Inactivo</option>
            </select>
          </div>

          <div className="pagination__controls">
            <button type="button" className="button button--primary" onClick={applyFilters}>
              Buscar
            </button>
            <button type="button" className="button button--secondary" onClick={clearFilters}>
              Limpiar
            </button>
          </div>
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

        {entitiesQuery.isLoading && <LoadingIndicator />}

        {!entitiesQuery.isLoading && entitiesQuery.data && (
          <>
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
                      <tr key={entity.id}>
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
                                onClick={() => {
                                  setOperationError(null);
                                  setSuccessMessage(null);
                                  setEntityBeingEdited(entity);
                                }}
                              >
                                <EditIcon />
                              </button>
                              <button
                                type="button"
                                className="button button--icon"
                                title="Eliminar"
                                aria-label={`Eliminar ${entity.name}`}
                                onClick={() => {
                                  setOperationError(null);
                                  setSuccessMessage(null);
                                  setEntityBeingDeleted(entity);
                                }}
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
                setAppliedFilter({ ...appliedFilter, pageNumber })
              }
              onPageSizeChange={(pageSize) =>
                setAppliedFilter({ ...appliedFilter, pageSize, pageNumber: 1 })
              }
            />
          </>
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
