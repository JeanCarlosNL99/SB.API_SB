import { useCallback, useState } from 'react';
import { departmentsApi, employeesApi } from '@/api/endpoints';
import { EmployeeForm } from '@/components/EmployeeForm';
import { EMPLOYEE_TYPE_DEFINITIONS } from '@/constants/employeeTypes';
import {
  EmptyState,
  ErrorMessage,
  LoadingIndicator,
  SuccessMessage,
} from '@/components/Feedback';
import { DetailIcon, EditIcon, PlusIcon, TrashIcon } from '@/components/Icons';
import { ConfirmationDialog, Modal } from '@/components/Modal';
import { Pagination } from '@/components/Pagination';
import { useAuthentication } from '@/hooks/useAuthentication';
import { useAsyncData } from '@/hooks/useAsyncData';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { formatCurrency, formatNumber, formatPercentage } from '@/utils/formatters';
import { buildClickableRowProps } from '@/utils/tableInteraction';
import type {
  Employee,
  EmployeeFilter,
  EmployeeRequest,
  EmployeeStatus,
  EmployeeType,
  PagedResponse,
} from '@/types/api';

const INITIAL_FILTER: EmployeeFilter = {
  name: '',
  departmentId: '',
  status: '',
  type: '',
  pageNumber: 1,
  pageSize: 10,
};

/**
 * Mantenimiento de empleados con los filtros exigidos por el requerimiento:
 * nombre, departamento y estado, mas el filtro por tipo de contrato.
 */
export function EmployeesPage() {
  const { canWriteMaintenance } = useAuthentication();

  const [filter, setFilter] = useState<EmployeeFilter>(INITIAL_FILTER);

  const [isCreating, setIsCreating] = useState(false);
  const [employeeBeingEdited, setEmployeeBeingEdited] = useState<Employee | null>(null);
  const [employeeBeingViewed, setEmployeeBeingViewed] = useState<Employee | null>(null);
  const [employeeBeingDeleted, setEmployeeBeingDeleted] = useState<Employee | null>(null);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [operationError, setOperationError] = useState<unknown>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  const departmentsQuery = useAsyncData(() => departmentsApi.getAll(), []);

  // El campo de texto responde de inmediato en pantalla, pero la consulta se
  // lanza cuando el usuario deja de escribir.
  const debouncedName = useDebouncedValue(filter.name ?? '');

  const employeesQuery = useAsyncData<PagedResponse<Employee>>(
    () => employeesApi.search({ ...filter, name: debouncedName }),
    [
      debouncedName,
      filter.departmentId,
      filter.status,
      filter.type,
      filter.pageNumber,
      filter.pageSize,
    ],
  );

  /**
   * Aplica un cambio de filtro y vuelve a la primera pagina: mantener la pagina
   * anterior mostraria un resultado vacio cuando el nuevo filtro devuelve menos
   * paginas que la actual.
   */
  const updateFilter = useCallback((changes: Partial<EmployeeFilter>) => {
    setFilter((previousFilter) => ({ ...previousFilter, ...changes, pageNumber: 1 }));
  }, []);

  const clearFilters = useCallback(() => {
    setFilter(INITIAL_FILTER);
  }, []);

  const hasActiveFilters =
    (filter.name ?? '') !== '' ||
    (filter.departmentId ?? '') !== '' ||
    (filter.status ?? '') !== '' ||
    (filter.type ?? '') !== '';

  // Ya hay datos en pantalla y se esta trayendo un resultado nuevo: se atenua la
  // tabla en lugar de sustituirla, para no parpadear en cada pulsacion.
  const isRefreshing = employeesQuery.isLoading && employeesQuery.data !== null;
  const isFirstLoad = employeesQuery.isLoading && employeesQuery.data === null;

  function resetMessages() {
    setSuccessMessage(null);
    setOperationError(null);
  }

  async function handleCreate(request: EmployeeRequest) {
    setIsProcessing(true);
    setOperationError(null);

    try {
      const createdEmployee = await employeesApi.create(request);

      setIsCreating(false);
      setSuccessMessage(
        `Empleado ${createdEmployee.fullName} registrado. Pago semanal calculado: ` +
          `${formatCurrency(createdEmployee.weeklyPayment)}.`,
      );

      await employeesQuery.reload();
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsProcessing(false);
    }
  }

  async function handleUpdate(request: EmployeeRequest) {
    if (!employeeBeingEdited) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      const updatedEmployee = await employeesApi.update(employeeBeingEdited.id, request);

      setEmployeeBeingEdited(null);
      setSuccessMessage(
        `Empleado ${updatedEmployee.fullName} actualizado. Nuevo pago semanal: ` +
          `${formatCurrency(updatedEmployee.weeklyPayment)}.`,
      );

      await employeesQuery.reload();
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsProcessing(false);
    }
  }

  async function handleDelete() {
    if (!employeeBeingDeleted) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      await employeesApi.remove(employeeBeingDeleted.id);
      setSuccessMessage(`Empleado ${employeeBeingDeleted.fullName} eliminado.`);
      setEmployeeBeingDeleted(null);
      await employeesQuery.reload();
    } catch (error) {
      setOperationError(error);
      setEmployeeBeingDeleted(null);
    } finally {
      setIsProcessing(false);
    }
  }

  async function openDetail(employee: Employee) {
    resetMessages();

    try {
      // Se solicita el empleado completo porque el listado no incluye el desglose
      // del calculo, para no transferir datos que la tabla no muestra.
      const detailedEmployee = await employeesApi.getById(employee.id);

      setEmployeeBeingViewed(detailedEmployee);
    } catch (error) {
      setOperationError(error);
    }
  }

  function openEditor(employee: Employee) {
    resetMessages();
    setEmployeeBeingEdited(employee);
  }

  function openDeleteConfirmation(employee: Employee) {
    resetMessages();
    setEmployeeBeingDeleted(employee);
  }

  /**
   * Accion asociada al clic sobre una fila. Quien puede editar abre el
   * formulario; quien solo tiene permiso de consulta abre el detalle del
   * calculo, que es la accion equivalente disponible para su rol.
   */
  function activateRow(employee: Employee) {
    if (canWriteMaintenance) {
      openEditor(employee);

      return;
    }

    void openDetail(employee);
  }

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Filtros de consulta</h2>
            <p className="card__description">
              Busque por nombre o apellido, departamento, estado y tipo de contrato. Los
              filtros se aplican al instante.
            </p>
          </div>
          <div className="pagination__controls">
            <button
              type="button"
              className="button button--secondary"
              onClick={clearFilters}
              disabled={!hasActiveFilters}
            >
              Limpiar filtros
            </button>
            {canWriteMaintenance && (
              <button
                type="button"
                className="button button--accent"
                onClick={() => {
                  resetMessages();
                  setIsCreating(true);
                }}
              >
                <PlusIcon size={16} /> Nuevo empleado
              </button>
            )}
          </div>
        </div>

        <div className="filters">
          <div className="field">
            <label className="field__label" htmlFor="employeeName">
              Nombre o apellido
            </label>
            <input
              id="employeeName"
              className="control"
              type="search"
              placeholder="Escriba para filtrar"
              autoComplete="off"
              value={filter.name ?? ''}
              onChange={(changeEvent) => updateFilter({ name: changeEvent.target.value })}
            />
          </div>

          <div className="field">
            <label className="field__label" htmlFor="employeeDepartment">
              Departamento
            </label>
            <select
              id="employeeDepartment"
              className="control"
              value={filter.departmentId ?? ''}
              onChange={(changeEvent) =>
                updateFilter({ departmentId: changeEvent.target.value })
              }
            >
              <option value="">Todos</option>
              {(departmentsQuery.data ?? []).map((department) => (
                <option key={department.id} value={department.id}>
                  {department.name}
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label className="field__label" htmlFor="employeeStatusFilter">
              Estado
            </label>
            <select
              id="employeeStatusFilter"
              className="control"
              value={filter.status ?? ''}
              onChange={(changeEvent) =>
                updateFilter({ status: changeEvent.target.value as EmployeeStatus | '' })
              }
            >
              <option value="">Todos</option>
              <option value="Active">Activo</option>
              <option value="Inactive">Inactivo</option>
            </select>
          </div>

          <div className="field">
            <label className="field__label" htmlFor="employeeTypeFilter">
              Tipo de contrato
            </label>
            <select
              id="employeeTypeFilter"
              className="control"
              value={filter.type ?? ''}
              onChange={(changeEvent) =>
                updateFilter({ type: changeEvent.target.value as EmployeeType | '' })
              }
            >
              <option value="">Todos</option>
              {EMPLOYEE_TYPE_DEFINITIONS.map((definition) => (
                <option key={definition.value} value={definition.value}>
                  {definition.label}
                </option>
              ))}
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
          {!employeesQuery.isLoading && employeesQuery.data && (
            <>{employeesQuery.data.totalCount} empleado(s) coinciden con el filtro actual.</>
          )}
        </div>
      </section>

      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Empleados</h2>
            <p className="card__description">
              El pago semanal lo calcula la API segun el tipo de contrato.
            </p>
          </div>
        </div>

        <SuccessMessage message={successMessage} />
        <ErrorMessage error={operationError ?? employeesQuery.error} />

        {isFirstLoad && <LoadingIndicator />}

        {employeesQuery.data && (
          <div className={isRefreshing ? 'is-refreshing' : undefined}>
            {employeesQuery.data.items.length === 0 ? (
              <EmptyState
                title="No se encontraron empleados"
                description="Ajuste los filtros o registre un nuevo empleado."
              />
            ) : (
              <div className="table-wrapper">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Empleado</th>
                      <th>Seguro social</th>
                      <th>Tipo de contrato</th>
                      <th>Departamento</th>
                      <th>Estado</th>
                      <th className="table th--numeric">Pago semanal</th>
                      <th aria-label="Acciones" />
                    </tr>
                  </thead>
                  <tbody>
                    {employeesQuery.data.items.map((employee) => (
                      <tr
                        key={employee.id}
                        {...buildClickableRowProps(
                          () => activateRow(employee),
                          canWriteMaintenance
                            ? `Editar ${employee.fullName}`
                            : `Ver detalle de ${employee.fullName}`,
                        )}
                      >
                        <td>{employee.fullName}</td>
                        <td>{employee.socialSecurityNumber}</td>
                        <td>
                          <span className="badge badge--type">
                            {employee.typeDescription}
                          </span>
                        </td>
                        <td>{employee.departmentName}</td>
                        <td>
                          <span
                            className={
                              employee.status === 'Active'
                                ? 'badge badge--active'
                                : 'badge badge--inactive'
                            }
                          >
                            {employee.statusDescription}
                          </span>
                        </td>
                        <td className="table td--numeric">
                          {formatCurrency(employee.weeklyPayment)}
                        </td>
                        <td>
                          <div className="table__actions">
                            <button
                              type="button"
                              className="button button--icon"
                              title="Ver detalle del calculo"
                              aria-label={`Ver detalle de ${employee.fullName}`}
                              onClick={() => void openDetail(employee)}
                            >
                              <DetailIcon />
                            </button>
                            {canWriteMaintenance && (
                              <>
                                <button
                                  type="button"
                                  className="button button--icon"
                                  title="Editar"
                                  aria-label={`Editar ${employee.fullName}`}
                                  onClick={() => openEditor(employee)}
                                >
                                  <EditIcon />
                                </button>
                                <button
                                  type="button"
                                  className="button button--icon"
                                  title="Eliminar"
                                  aria-label={`Eliminar ${employee.fullName}`}
                                  onClick={() => openDeleteConfirmation(employee)}
                                >
                                  <TrashIcon />
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <Pagination
              page={employeesQuery.data}
              onPageChange={(pageNumber) =>
                setFilter((previousFilter) => ({ ...previousFilter, pageNumber }))
              }
              onPageSizeChange={(pageSize) => updateFilter({ pageSize })}
            />
          </div>
        )}
      </section>

      <Modal
        title="Nuevo empleado"
        description="Los campos solicitados cambian segun el tipo de contrato."
        isOpen={isCreating}
        onClose={() => setIsCreating(false)}
      >
        <EmployeeForm
          departments={departmentsQuery.data ?? []}
          isSubmitting={isProcessing}
          submitError={operationError}
          onSubmit={handleCreate}
          onCancel={() => setIsCreating(false)}
        />
      </Modal>

      <Modal
        title="Editar empleado"
        description="Al guardar, la API recalcula el pago semanal con los nuevos valores."
        isOpen={employeeBeingEdited !== null}
        onClose={() => setEmployeeBeingEdited(null)}
      >
        {employeeBeingEdited && (
          <EmployeeForm
            employee={employeeBeingEdited}
            departments={departmentsQuery.data ?? []}
            isSubmitting={isProcessing}
            submitError={operationError}
            onSubmit={handleUpdate}
            onCancel={() => setEmployeeBeingEdited(null)}
          />
        )}
      </Modal>

      <Modal
        title="Detalle del calculo de pago"
        isOpen={employeeBeingViewed !== null}
        onClose={() => setEmployeeBeingViewed(null)}
      >
        {employeeBeingViewed && <EmployeeDetail employee={employeeBeingViewed} />}
      </Modal>

      <ConfirmationDialog
        isOpen={employeeBeingDeleted !== null}
        title="Eliminar empleado"
        message={`Se eliminara a ${employeeBeingDeleted?.fullName ?? ''}. Esta accion no se puede deshacer.`}
        isProcessing={isProcessing}
        onConfirm={handleDelete}
        onCancel={() => setEmployeeBeingDeleted(null)}
      />
    </>
  );
}

/** Detalle de un empleado con el desglose del calculo devuelto por la API. */
function EmployeeDetail({ employee }: { employee: Employee }) {
  return (
    <div>
      <div className="detail-row">
        <span className="detail-row__label">Empleado</span>
        <span className="detail-row__value">{employee.fullName}</span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Numero de seguro social</span>
        <span className="detail-row__value">{employee.socialSecurityNumber}</span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Tipo de contrato</span>
        <span className="detail-row__value">{employee.typeDescription}</span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Departamento</span>
        <span className="detail-row__value">{employee.departmentName}</span>
      </div>

      {employee.weeklySalary !== null && employee.weeklySalary !== undefined && (
        <div className="detail-row">
          <span className="detail-row__label">Salario semanal</span>
          <span className="detail-row__value">{formatCurrency(employee.weeklySalary)}</span>
        </div>
      )}
      {employee.hourlyWage !== null && employee.hourlyWage !== undefined && (
        <div className="detail-row">
          <span className="detail-row__label">Sueldo por hora</span>
          <span className="detail-row__value">{formatCurrency(employee.hourlyWage)}</span>
        </div>
      )}
      {employee.hoursWorked !== null && employee.hoursWorked !== undefined && (
        <div className="detail-row">
          <span className="detail-row__label">Horas trabajadas</span>
          <span className="detail-row__value">{formatNumber(employee.hoursWorked)}</span>
        </div>
      )}
      {employee.grossSales !== null && employee.grossSales !== undefined && (
        <div className="detail-row">
          <span className="detail-row__label">Ventas brutas</span>
          <span className="detail-row__value">{formatCurrency(employee.grossSales)}</span>
        </div>
      )}
      {employee.commissionRate !== null && employee.commissionRate !== undefined && (
        <div className="detail-row">
          <span className="detail-row__label">Tarifa de comision</span>
          <span className="detail-row__value">
            {formatPercentage(employee.commissionRate)}
          </span>
        </div>
      )}
      {employee.baseSalary !== null && employee.baseSalary !== undefined && (
        <div className="detail-row">
          <span className="detail-row__label">Salario base</span>
          <span className="detail-row__value">{formatCurrency(employee.baseSalary)}</span>
        </div>
      )}

      {employee.paymentBreakdown && (
        <div style={{ marginTop: 20 }}>
          <p className="section-title">Formula aplicada</p>
          <code className="formula">{employee.paymentBreakdown.formula}</code>

          <p className="section-title" style={{ marginTop: 16 }}>
            Desglose
          </p>
          <ul className="breakdown">
            {employee.paymentBreakdown.components.map((component) => (
              <li className="breakdown__item" key={component.concept}>
                <span>
                  <span className="breakdown__concept">{component.concept}</span>
                  <br />
                  <span className="breakdown__detail">{component.detail}</span>
                </span>
                <span className="breakdown__amount">
                  {formatCurrency(component.amount)}
                </span>
              </li>
            ))}
          </ul>

          <div className="detail-row" style={{ marginTop: 12 }}>
            <span className="detail-row__label">Total del pago semanal</span>
            <span className="detail-row__value">
              {formatCurrency(employee.paymentBreakdown.totalAmount)}
            </span>
          </div>
        </div>
      )}
    </div>
  );
}
