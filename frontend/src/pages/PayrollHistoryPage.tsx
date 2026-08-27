import { useCallback, useState } from 'react';
import { companiesApi, payrollApi } from '@/api/payrollEndpoints';
import {
  EmptyState,
  ErrorMessage,
  LoadingIndicator,
  SuccessMessage,
} from '@/components/Feedback';
import { PayrollRunDetailView } from '@/components/PayrollRunDetailView';
import { Modal } from '@/components/Modal';
import { Pagination } from '@/components/Pagination';
import { useAuthentication } from '@/hooks/useAuthentication';
import { useAsyncData } from '@/hooks/useAsyncData';
import { formatCurrency, formatDate, formatDateTime } from '@/utils/formatters';
import { buildClickableRowProps } from '@/utils/tableInteraction';
import { getCurrentWeek } from '@/utils/payrollWeek';
import type {
  Company,
  PagedResponse,
  PayrollRunDetail,
  PayrollRunFilter,
  PayrollRunSummary,
} from '@/types/api';

const MINIMUM_CANCELLATION_REASON_LENGTH = 10;

const INITIAL_FILTER: PayrollRunFilter = {
  companyId: '',
  year: '',
  includeCancelled: true,
  pageNumber: 1,
  pageSize: 10,
};

/**
 * Historial de nominas generadas.
 *
 * Es el reporte de los calculos de semanas anteriores. Cada documento conserva la
 * instantanea de lo que se pago, por lo que abrir una nomina de hace dos meses
 * muestra los montos de entonces y no un recalculo con los datos de hoy.
 */
export function PayrollHistoryPage() {
  const { isAdministrator } = useAuthentication();

  const [filter, setFilter] = useState<PayrollRunFilter>(INITIAL_FILTER);
  const [selectedRun, setSelectedRun] = useState<PayrollRunDetail | null>(null);
  const [runBeingCancelled, setRunBeingCancelled] = useState<PayrollRunSummary | null>(null);
  const [cancellationReason, setCancellationReason] = useState('');
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [operationError, setOperationError] = useState<unknown>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  const companiesQuery = useAsyncData<Company[]>(() => companiesApi.getAll(), []);

  const historyQuery = useAsyncData<PagedResponse<PayrollRunSummary>>(
    () => payrollApi.searchHistory(filter),
    [
      filter.companyId,
      filter.year,
      filter.includeCancelled,
      filter.pageNumber,
      filter.pageSize,
    ],
  );

  const updateFilter = useCallback((changes: Partial<PayrollRunFilter>) => {
    setFilter((previousFilter) => ({ ...previousFilter, ...changes, pageNumber: 1 }));
  }, []);

  const availableYears = buildAvailableYears();

  async function openDetail(summary: PayrollRunSummary) {
    setOperationError(null);
    setSuccessMessage(null);

    try {
      const detail = await payrollApi.getById(summary.id);

      setSelectedRun(detail);
    } catch (error) {
      setOperationError(error);
    }
  }

  async function handleCancel() {
    if (runBeingCancelled === null) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      await payrollApi.cancel(runBeingCancelled.id, { reason: cancellationReason.trim() });

      setSuccessMessage(
        `La nomina de la semana ${runBeingCancelled.weekLabel} de ` +
          `${runBeingCancelled.companyName} fue anulada. La semana queda libre para ` +
          'volver a calcularse.',
      );

      setRunBeingCancelled(null);
      setCancellationReason('');
      await historyQuery.reload();
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsProcessing(false);
    }
  }

  const isReasonValid =
    cancellationReason.trim().length >= MINIMUM_CANCELLATION_REASON_LENGTH;

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Filtros del historial</h2>
            <p className="card__description">
              Cada registro es la nomina de una compania para una semana concreta.
            </p>
          </div>
        </div>

        <div className="filters">
          <div className="field">
            <label className="field__label" htmlFor="historyCompany">
              Compania
            </label>
            <select
              id="historyCompany"
              className="control"
              value={filter.companyId ?? ''}
              onChange={(changeEvent) =>
                updateFilter({ companyId: changeEvent.target.value })
              }
            >
              <option value="">Todas</option>
              {(companiesQuery.data ?? []).map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label className="field__label" htmlFor="historyYear">
              Ano
            </label>
            <select
              id="historyYear"
              className="control"
              value={filter.year === '' || filter.year === undefined ? '' : filter.year}
              onChange={(changeEvent) =>
                updateFilter({
                  year: changeEvent.target.value === '' ? '' : Number(changeEvent.target.value),
                })
              }
            >
              <option value="">Todos</option>
              {availableYears.map((year) => (
                <option key={year} value={year}>
                  {year}
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label className="field__label" htmlFor="historyIncludeCancelled">
              Anuladas
            </label>
            <select
              id="historyIncludeCancelled"
              className="control"
              value={filter.includeCancelled ? 'include' : 'exclude'}
              onChange={(changeEvent) =>
                updateFilter({ includeCancelled: changeEvent.target.value === 'include' })
              }
            >
              <option value="include">Incluir anuladas</option>
              <option value="exclude">Solo vigentes</option>
            </select>
          </div>
        </div>
      </section>

      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Nominas generadas</h2>
            <p className="card__description">
              De la mas reciente a la mas antigua. Cada documento conserva lo que se pago en
              su momento.
            </p>
          </div>
        </div>

        <SuccessMessage message={successMessage} />
        <ErrorMessage error={operationError ?? historyQuery.error} />

        {historyQuery.isLoading && historyQuery.data === null && <LoadingIndicator />}

        {historyQuery.data && (
          <div className={historyQuery.isLoading ? 'is-refreshing' : undefined}>
            {historyQuery.data.items.length === 0 ? (
              <EmptyState
                title="No hay nominas en el historial"
                description="Genere el pago de una semana desde la pantalla de calculo de nomina."
              />
            ) : (
              <div className="table-wrapper">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Semana</th>
                      <th>Periodo</th>
                      <th>Compania</th>
                      <th className="table th--numeric">Empleados</th>
                      <th className="table th--numeric">Total pagado</th>
                      <th>Estado</th>
                      <th>Generada</th>
                      <th aria-label="Acciones" />
                    </tr>
                  </thead>
                  <tbody>
                    {historyQuery.data.items.map((summary) => (
                      <tr
                        key={summary.id}
                        {...buildClickableRowProps(
                          () => void openDetail(summary),
                          `Ver la nomina ${summary.weekLabel} de ${summary.companyName}`,
                        )}
                      >
                        <td>
                          <strong>{summary.weekLabel}</strong>
                        </td>
                        <td>
                          {formatDate(summary.weekStartDate)} —{' '}
                          {formatDate(summary.weekEndDate)}
                        </td>
                        <td className="table td--wrap">{summary.companyName}</td>
                        <td className="table td--numeric">{summary.employeeCount}</td>
                        <td className="table td--numeric">
                          {formatCurrency(summary.totalAmount)}
                        </td>
                        <td>
                          <span
                            className={
                              summary.status === 'Generated'
                                ? 'badge badge--active'
                                : 'badge badge--inactive'
                            }
                          >
                            {summary.statusDescription}
                          </span>
                        </td>
                        <td>{formatDateTime(summary.generatedAt)}</td>
                        <td>
                          <div className="table__actions">
                            <button
                              type="button"
                              className="button button--secondary"
                              onClick={() => void openDetail(summary)}
                            >
                              Ver detalle
                            </button>
                            {isAdministrator && summary.status === 'Generated' && (
                              <button
                                type="button"
                                className="button button--danger"
                                onClick={() => {
                                  setOperationError(null);
                                  setSuccessMessage(null);
                                  setCancellationReason('');
                                  setRunBeingCancelled(summary);
                                }}
                              >
                                Anular
                              </button>
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
              page={historyQuery.data}
              onPageChange={(pageNumber) =>
                setFilter((previousFilter) => ({ ...previousFilter, pageNumber }))
              }
              onPageSizeChange={(pageSize) => updateFilter({ pageSize })}
            />
          </div>
        )}
      </section>

      <Modal
        title="Detalle de la nomina"
        description="Instantanea del pago tal como quedo el dia en que se genero."
        isOpen={selectedRun !== null}
        onClose={() => setSelectedRun(null)}
      >
        {selectedRun && <PayrollRunDetailView detail={selectedRun} />}
      </Modal>

      <Modal
        title="Anular la nomina"
        description="El documento se conserva como evidencia y la semana queda libre para volver a calcularse."
        isOpen={runBeingCancelled !== null}
        onClose={() => setRunBeingCancelled(null)}
      >
        {runBeingCancelled && (
          <div>
            <p>
              Se anulara la nomina de la semana <strong>{runBeingCancelled.weekLabel}</strong> de{' '}
              <strong>{runBeingCancelled.companyName}</strong>, por{' '}
              {formatCurrency(runBeingCancelled.totalAmount)}.
            </p>

            <div className="field" style={{ marginTop: 16 }}>
              <label className="field__label" htmlFor="cancellationReason">
                Motivo de la anulacion
              </label>
              <textarea
                id="cancellationReason"
                className="control"
                rows={3}
                value={cancellationReason}
                onChange={(changeEvent) => setCancellationReason(changeEvent.target.value)}
              />
              <span className="field__hint">
                Minimo {MINIMUM_CANCELLATION_REASON_LENGTH} caracteres. Queda registrado en el
                historial.
              </span>
            </div>

            <ErrorMessage error={operationError} />

            <div className="form-actions">
              <button
                type="button"
                className="button button--secondary"
                onClick={() => setRunBeingCancelled(null)}
                disabled={isProcessing}
              >
                Cancelar
              </button>
              <button
                type="button"
                className="button button--danger"
                onClick={() => void handleCancel()}
                disabled={!isReasonValid || isProcessing}
              >
                {isProcessing ? 'Anulando...' : 'Anular nomina'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}

/**
 * Construye la lista de anos ofrecidos en el filtro: el actual y los cuatro
 * anteriores, que es el rango en el que puede haber historial.
 */
function buildAvailableYears(): number[] {
  const currentYear = getCurrentWeek().year;

  return [0, 1, 2, 3, 4].map((offset) => currentYear - offset);
}
