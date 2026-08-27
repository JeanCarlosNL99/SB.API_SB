import { useCallback, useMemo, useState } from 'react';
import { payrollApi } from '@/api/payrollEndpoints';
import {
  EmptyState,
  ErrorMessage,
  LoadingIndicator,
  SuccessMessage,
} from '@/components/Feedback';
import { PayrollLinesTable } from '@/components/PayrollLinesTable';
import { PayrollRunDetailView } from '@/components/PayrollRunDetailView';
import { PayrollIcon } from '@/components/Icons';
import { Modal } from '@/components/Modal';
import { useAuthentication } from '@/hooks/useAuthentication';
import { useAsyncData } from '@/hooks/useAsyncData';
import { formatCurrency, formatDate } from '@/utils/formatters';
import {
  fromInputValue,
  getCurrentWeek,
  getPreviousWeek,
  toInputValue,
  type PayrollWeekValue,
} from '@/utils/payrollWeek';
import type {
  PayableGovernmentEntity,
  PayrollPreview,
  PayrollRunDetail,
} from '@/types/api';

/**
 * Calculo de pagos semanales.
 *
 * La pantalla materializa el flujo de trabajo del negocio: se elige la entidad
 * y la semana, se revisa el calculo propuesto y solo entonces se genera. Si la
 * semana ya fue pagada, el boton de generar queda deshabilitado y se ofrece el
 * enlace a la nomina existente: se impide el error en lugar de reportarlo despues.
 */
export function PayrollPage() {
  const { canWriteMaintenance } = useAuthentication();

  // Por omision se propone la semana anterior: la semana en curso todavia no ha
  // terminado y sus horas no estan completas.
  const [selectedWeek, setSelectedWeek] = useState<PayrollWeekValue>(() =>
    getPreviousWeek(getCurrentWeek()),
  );

  const [selectedGovernmentEntityId, setSelectedGovernmentEntityId] = useState('');
  const [onlyActiveEmployees, setOnlyActiveEmployees] = useState(true);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [operationError, setOperationError] = useState<unknown>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const [generatedRun, setGeneratedRun] = useState<PayrollRunDetail | null>(null);
  const [reloadToken, setReloadToken] = useState(0);

  // El selector no ofrece las 181 entidades del listado oficial, sino las que
  // tienen empleados: una entidad sin empleados solo produce un calculo vacio que
  // la propia API rechaza.
  const governmentEntitiesQuery = useAsyncData<PayableGovernmentEntity[]>(async () => {
    const payableEntities = await payrollApi.getPayableEntities();

    // Se preselecciona la primera entidad con empleados activos para que la
    // pantalla muestre informacion util sin exigir una interaccion previa.
    setSelectedGovernmentEntityId((previousGovernmentEntityId) => {
      if (previousGovernmentEntityId !== '') {
        return previousGovernmentEntityId;
      }

      return (
        payableEntities.find((entity) => entity.activeEmployeeCount > 0)?.id ?? ''
      );
    });

    return payableEntities;
  }, []);

  const previewQuery = useAsyncData<PayrollPreview | null>(
    () =>
      selectedGovernmentEntityId === ''
        ? Promise.resolve(null)
        : payrollApi.getPreview(
            selectedGovernmentEntityId,
            selectedWeek.year,
            selectedWeek.weekNumber,
            onlyActiveEmployees,
          ),
    [
      selectedGovernmentEntityId,
      selectedWeek.year,
      selectedWeek.weekNumber,
      onlyActiveEmployees,
      reloadToken,
    ],
  );

  const selectedGovernmentEntity = useMemo(
    () =>
      (governmentEntitiesQuery.data ?? []).find(
        (entity) => entity.id === selectedGovernmentEntityId,
      ),
    [governmentEntitiesQuery.data, selectedGovernmentEntityId],
  );

  const handleWeekChange = useCallback((inputValue: string) => {
    const week = fromInputValue(inputValue);

    if (week !== null) {
      setSuccessMessage(null);
      setOperationError(null);
      setSelectedWeek(week);
    }
  }, []);

  async function handleGenerate() {
    if (selectedGovernmentEntityId === '') {
      return;
    }

    setIsGenerating(true);
    setOperationError(null);
    setSuccessMessage(null);

    try {
      const detail = await payrollApi.generate({
        governmentEntityId: selectedGovernmentEntityId,
        year: selectedWeek.year,
        weekNumber: selectedWeek.weekNumber,
        onlyActiveEmployees,
      });

      setGeneratedRun(detail);
      setSuccessMessage(
        `Nomina de la semana ${detail.summary.weekLabel} generada para ` +
          `${detail.summary.governmentEntityName}. Total pagado: ` +
          `${formatCurrency(detail.summary.totalAmount)}.`,
      );

      // Se recarga la vista previa para que refleje que la semana quedo pagada.
      setReloadToken((previousToken) => previousToken + 1);
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsGenerating(false);
    }
  }

  const preview = previewQuery.data;
  const canGenerate =
    canWriteMaintenance &&
    preview !== null &&
    preview !== undefined &&
    !preview.isAlreadyGenerated &&
    preview.employeeCount > 0 &&
    !isGenerating;

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Calculo de pagos de la semana</h2>
            <p className="card__description">
              Seleccione la entidad gubernamental y la semana, revise el calculo y
              genere el pago. Una semana solo puede generarse una vez por entidad.
            </p>
          </div>
        </div>

        <div className="filters">
          <div className="field">
            <label className="field__label" htmlFor="payrollGovernmentEntity">
              Entidad gubernamental
            </label>
            <select
              id="payrollGovernmentEntity"
              className="control"
              value={selectedGovernmentEntityId}
              onChange={(changeEvent) => {
                setSuccessMessage(null);
                setOperationError(null);
                setSelectedGovernmentEntityId(changeEvent.target.value);
              }}
            >
              <option value="">Seleccione una entidad gubernamental</option>
              {(governmentEntitiesQuery.data ?? []).map((entity) => (
                <option key={entity.id} value={entity.id}>
                  {entity.name} ({entity.activeEmployeeCount} activos)
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label className="field__label" htmlFor="payrollWeek">
              Semana a pagar
            </label>
            <input
              id="payrollWeek"
              className="control"
              type="week"
              value={toInputValue(selectedWeek)}
              onChange={(changeEvent) => handleWeekChange(changeEvent.target.value)}
            />
            {preview && (
              <span className="field__hint">
                Del {formatDate(preview.weekStartDate)} al {formatDate(preview.weekEndDate)}
              </span>
            )}
          </div>

          <div className="field">
            <label className="field__label" htmlFor="payrollScope">
              Alcance
            </label>
            <select
              id="payrollScope"
              className="control"
              value={onlyActiveEmployees ? 'active' : 'all'}
              onChange={(changeEvent) =>
                setOnlyActiveEmployees(changeEvent.target.value === 'active')
              }
            >
              <option value="active">Solo empleados activos</option>
              <option value="all">Todos los empleados</option>
            </select>
          </div>
        </div>

        <SuccessMessage message={successMessage} />
        <ErrorMessage error={operationError} />
      </section>

      {selectedGovernmentEntityId === '' && (
        <section className="card">
          <EmptyState
            title="Seleccione una entidad gubernamental"
            description="El calculo de la nomina se realiza por entidad y por semana."
          />
        </section>
      )}

      {selectedGovernmentEntityId !== '' && previewQuery.isLoading && (
        <section className="card">
          <LoadingIndicator label="Calculando la nomina de la semana..." />
        </section>
      )}

      {selectedGovernmentEntityId !== '' && previewQuery.error && (
        <section className="card">
          <ErrorMessage error={previewQuery.error} />
        </section>
      )}

      {preview && !previewQuery.isLoading && (
        <>
          <section className="card">
            <div className="card__header">
              <div>
                <h2 className="card__title">
                  {preview.isAlreadyGenerated
                    ? `Semana ${preview.weekLabel}: ya pagada`
                    : `Semana ${preview.weekLabel}: calculo propuesto`}
                </h2>
                <p className="card__description">
                  {preview.isAlreadyGenerated
                    ? 'Esta semana ya tiene nomina generada. Para volver a calcularla, un ' +
                      'administrador debe anular la ejecucion existente desde el historial.'
                    : 'El calculo todavia no se ha guardado. Revise los montos antes de generar.'}
                </p>
              </div>

              {canWriteMaintenance && (
                <button
                  type="button"
                  className="button button--accent"
                  onClick={() => void handleGenerate()}
                  disabled={!canGenerate}
                  title={
                    preview.isAlreadyGenerated
                      ? 'La semana ya tiene nomina generada'
                      : preview.employeeCount === 0
                        ? 'La entidad no tiene empleados que incluir'
                        : 'Generar el pago de la semana'
                  }
                >
                  <PayrollIcon size={16} />
                  {isGenerating ? 'Generando...' : 'Generar pago semanal'}
                </button>
              )}
            </div>

            {preview.isAlreadyGenerated && (
              <div className="alert alert--info" role="status">
                <div>
                  <strong>La semana {preview.weekLabel} ya fue pagada.</strong>
                  <p style={{ marginTop: 4 }}>
                    Consulte el historial para ver el documento generado. El calculo que se
                    muestra abajo corresponde a los datos vigentes de los empleados y puede
                    diferir de lo que se pago.
                  </p>
                </div>
              </div>
            )}

            <div className="metric-grid" style={{ marginTop: 16 }}>
              <div className="metric-card">
                <span className="metric-card__icon">
                  <PayrollIcon size={22} />
                </span>
                <div>
                  <p className="metric-card__label">Entidad gubernamental</p>
                  <p style={{ fontWeight: 600 }}>
                    {selectedGovernmentEntity?.name ?? preview.governmentEntityName}
                  </p>
                </div>
              </div>
              <div className="metric-card">
                <div>
                  <p className="metric-card__label">Empleados a pagar</p>
                  <p className="metric-card__value">{preview.employeeCount}</p>
                </div>
              </div>
              <div className="metric-card">
                <div>
                  <p className="metric-card__label">Total a pagar</p>
                  <p className="metric-card__value">{formatCurrency(preview.totalAmount)}</p>
                </div>
              </div>
            </div>
          </section>

          <section className="card">
            <div className="card__header">
              <div>
                <h2 className="card__title">Detalle del calculo por empleado</h2>
                <p className="card__description">
                  Seleccione una fila para ver la formula aplicada y su desglose.
                </p>
              </div>
            </div>

            {preview.employeeCount === 0 ? (
              <EmptyState
                title="La entidad no tiene empleados que incluir"
                description="Registre empleados activos en esta entidad o cambie el alcance a todos los empleados."
              />
            ) : (
              <PayrollLinesTable lines={preview.lines} totalAmount={preview.totalAmount} />
            )}
          </section>
        </>
      )}

      <Modal
        title="Nomina generada"
        description="Este es el documento que quedo almacenado en el historial."
        isOpen={generatedRun !== null}
        onClose={() => setGeneratedRun(null)}
      >
        {generatedRun && <PayrollRunDetailView detail={generatedRun} />}
      </Modal>
    </>
  );
}
