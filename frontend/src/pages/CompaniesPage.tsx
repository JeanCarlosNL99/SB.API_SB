import { useState, type FormEvent } from 'react';
import { companiesApi } from '@/api/payrollEndpoints';
import {
  EmptyState,
  ErrorMessage,
  LoadingIndicator,
  SuccessMessage,
} from '@/components/Feedback';
import { EditIcon, PlusIcon, TrashIcon } from '@/components/Icons';
import { ConfirmationDialog, Modal } from '@/components/Modal';
import { useAuthentication } from '@/hooks/useAuthentication';
import { useAsyncData } from '@/hooks/useAsyncData';
import { formatDate } from '@/utils/formatters';
import { buildClickableRowProps } from '@/utils/tableInteraction';
import type { Company } from '@/types/api';

const MAXIMUM_NAME_LENGTH = 200;
const MAXIMUM_TAX_IDENTIFICATION_LENGTH = 20;
const TAX_IDENTIFICATION_PATTERN = /^[0-9A-Za-z-]+$/;

/** Valores del formulario de compania. */
interface CompanyFormValues {
  name: string;
  taxIdentificationNumber: string;
  isActive: boolean;
}

/**
 * Mantenimiento de companias. Es el registro base del modulo de nomina: cada
 * empleado pertenece a una compania y cada pago semanal se calcula por compania.
 */
export function CompaniesPage() {
  const { canWriteMaintenance, isAdministrator } = useAuthentication();

  const companiesQuery = useAsyncData<Company[]>(() => companiesApi.getAll(), []);

  const [isCreating, setIsCreating] = useState(false);
  const [companyBeingEdited, setCompanyBeingEdited] = useState<Company | null>(null);
  const [companyBeingDeleted, setCompanyBeingDeleted] = useState<Company | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [operationError, setOperationError] = useState<unknown>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  function resetMessages() {
    setSuccessMessage(null);
    setOperationError(null);
  }

  async function handleCreate(values: CompanyFormValues) {
    setIsProcessing(true);
    setOperationError(null);

    try {
      const created = await companiesApi.create({
        name: values.name.trim(),
        taxIdentificationNumber: values.taxIdentificationNumber.trim(),
      });

      setIsCreating(false);
      setSuccessMessage(`Compania "${created.name}" registrada correctamente.`);
      await companiesQuery.reload();
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsProcessing(false);
    }
  }

  async function handleUpdate(values: CompanyFormValues) {
    if (companyBeingEdited === null) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      await companiesApi.update(companyBeingEdited.id, {
        name: values.name.trim(),
        taxIdentificationNumber: values.taxIdentificationNumber.trim(),
        isActive: values.isActive,
      });

      setCompanyBeingEdited(null);
      setSuccessMessage(`Compania "${values.name}" actualizada correctamente.`);
      await companiesQuery.reload();
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsProcessing(false);
    }
  }

  async function handleDelete() {
    if (companyBeingDeleted === null) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      await companiesApi.remove(companyBeingDeleted.id);
      setSuccessMessage(`Compania "${companyBeingDeleted.name}" eliminada.`);
      setCompanyBeingDeleted(null);
      await companiesQuery.reload();
    } catch (error) {
      setOperationError(error);
      setCompanyBeingDeleted(null);
    } finally {
      setIsProcessing(false);
    }
  }

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Companias registradas</h2>
            <p className="card__description">
              Cada empleado pertenece a una compania y su nomina se calcula por compania y
              semana.
            </p>
          </div>
          {canWriteMaintenance && (
            <button
              type="button"
              className="button button--accent"
              onClick={() => {
                resetMessages();
                setIsCreating(true);
              }}
            >
              <PlusIcon size={16} /> Nueva compania
            </button>
          )}
        </div>

        <SuccessMessage message={successMessage} />
        <ErrorMessage error={operationError ?? companiesQuery.error} />

        {companiesQuery.isLoading && <LoadingIndicator />}

        {!companiesQuery.isLoading && companiesQuery.data && (
          <>
            {companiesQuery.data.length === 0 ? (
              <EmptyState
                title="No hay companias registradas"
                description="Registre una compania para poder capturar empleados y calcular su nomina."
              />
            ) : (
              <div className="table-wrapper">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Razon social</th>
                      <th>Registro (RNC)</th>
                      <th className="table th--numeric">Empleados activos</th>
                      <th>Estado</th>
                      <th>Registrada</th>
                      {canWriteMaintenance && <th aria-label="Acciones" />}
                    </tr>
                  </thead>
                  <tbody>
                    {companiesQuery.data.map((company) => (
                      <tr
                        key={company.id}
                        {...(canWriteMaintenance
                          ? buildClickableRowProps(
                              () => {
                                resetMessages();
                                setCompanyBeingEdited(company);
                              },
                              `Editar ${company.name}`,
                            )
                          : {})}
                      >
                        <td className="table td--wrap">{company.name}</td>
                        <td>{company.taxIdentificationNumber}</td>
                        <td className="table td--numeric">{company.activeEmployeeCount}</td>
                        <td>
                          <span
                            className={
                              company.isActive
                                ? 'badge badge--active'
                                : 'badge badge--inactive'
                            }
                          >
                            {company.isActive ? 'Activa' : 'Inactiva'}
                          </span>
                        </td>
                        <td>{formatDate(company.createdAt)}</td>
                        {canWriteMaintenance && (
                          <td>
                            <div className="table__actions">
                              <button
                                type="button"
                                className="button button--icon"
                                title="Editar"
                                aria-label={`Editar ${company.name}`}
                                onClick={() => {
                                  resetMessages();
                                  setCompanyBeingEdited(company);
                                }}
                              >
                                <EditIcon />
                              </button>
                              {isAdministrator && (
                                <button
                                  type="button"
                                  className="button button--icon"
                                  title={
                                    company.activeEmployeeCount > 0
                                      ? 'No se puede eliminar una compania con empleados'
                                      : 'Eliminar'
                                  }
                                  aria-label={`Eliminar ${company.name}`}
                                  onClick={() => {
                                    resetMessages();
                                    setCompanyBeingDeleted(company);
                                  }}
                                >
                                  <TrashIcon />
                                </button>
                              )}
                            </div>
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </section>

      <Modal
        title="Nueva compania"
        isOpen={isCreating}
        onClose={() => setIsCreating(false)}
      >
        <CompanyForm
          isSubmitting={isProcessing}
          submitError={operationError}
          onSubmit={handleCreate}
          onCancel={() => setIsCreating(false)}
        />
      </Modal>

      <Modal
        title="Editar compania"
        isOpen={companyBeingEdited !== null}
        onClose={() => setCompanyBeingEdited(null)}
      >
        {companyBeingEdited && (
          <CompanyForm
            company={companyBeingEdited}
            isSubmitting={isProcessing}
            submitError={operationError}
            onSubmit={handleUpdate}
            onCancel={() => setCompanyBeingEdited(null)}
          />
        )}
      </Modal>

      <ConfirmationDialog
        isOpen={companyBeingDeleted !== null}
        title="Eliminar compania"
        message={`Se eliminara "${companyBeingDeleted?.name ?? ''}". Solo se permite si no tiene empleados ni nominas registradas.`}
        isProcessing={isProcessing}
        onConfirm={handleDelete}
        onCancel={() => setCompanyBeingDeleted(null)}
      />
    </>
  );
}

function CompanyForm({
  company,
  isSubmitting,
  submitError,
  onSubmit,
  onCancel,
}: {
  company?: Company | null;
  isSubmitting: boolean;
  submitError: unknown;
  onSubmit: (values: CompanyFormValues) => Promise<void>;
  onCancel: () => void;
}) {
  const isEditing = company !== null && company !== undefined;

  const [values, setValues] = useState<CompanyFormValues>(
    company
      ? {
          name: company.name,
          taxIdentificationNumber: company.taxIdentificationNumber,
          isActive: company.isActive,
        }
      : { name: '', taxIdentificationNumber: '', isActive: true },
  );

  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  async function handleSubmit(formEvent: FormEvent<HTMLFormElement>) {
    formEvent.preventDefault();

    const errors: Record<string, string> = {};

    if (values.name.trim().length === 0) {
      errors.name = 'La razon social es obligatoria.';
    }

    const taxIdentification = values.taxIdentificationNumber.trim();

    if (taxIdentification.length === 0) {
      errors.taxIdentificationNumber = 'El Registro Nacional de Contribuyente es obligatorio.';
    } else if (!TAX_IDENTIFICATION_PATTERN.test(taxIdentification)) {
      errors.taxIdentificationNumber = 'Solo admite letras, numeros y guiones.';
    }

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);

      return;
    }

    await onSubmit(values);
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <ErrorMessage error={submitError} />

      <div className="form-grid">
        <div className="field" style={{ gridColumn: '1 / -1' }}>
          <label className="field__label" htmlFor="companyName">
            Razon social
          </label>
          <input
            id="companyName"
            className={`control${validationErrors.name ? ' control--invalid' : ''}`}
            type="text"
            maxLength={MAXIMUM_NAME_LENGTH}
            value={values.name}
            onChange={(changeEvent) => {
              setValues({ ...values, name: changeEvent.target.value });
              setValidationErrors({ ...validationErrors, name: '' });
            }}
          />
          {validationErrors.name && (
            <span className="field__error">{validationErrors.name}</span>
          )}
        </div>

        <div className="field">
          <label className="field__label" htmlFor="companyTaxIdentification">
            Registro Nacional de Contribuyente
          </label>
          <input
            id="companyTaxIdentification"
            className={`control${
              validationErrors.taxIdentificationNumber ? ' control--invalid' : ''
            }`}
            type="text"
            maxLength={MAXIMUM_TAX_IDENTIFICATION_LENGTH}
            placeholder="101-00001-1"
            value={values.taxIdentificationNumber}
            onChange={(changeEvent) => {
              setValues({ ...values, taxIdentificationNumber: changeEvent.target.value });
              setValidationErrors({ ...validationErrors, taxIdentificationNumber: '' });
            }}
          />
          {validationErrors.taxIdentificationNumber && (
            <span className="field__error">{validationErrors.taxIdentificationNumber}</span>
          )}
        </div>

        {isEditing && (
          <div className="field">
            <label className="field__label" htmlFor="companyIsActive">
              Estado
            </label>
            <select
              id="companyIsActive"
              className="control"
              value={values.isActive ? 'true' : 'false'}
              onChange={(changeEvent) =>
                setValues({ ...values, isActive: changeEvent.target.value === 'true' })
              }
            >
              <option value="true">Activa</option>
              <option value="false">Inactiva</option>
            </select>
            <span className="field__hint">
              Una compania inactiva no admite nuevos empleados ni generacion de nomina.
            </span>
          </div>
        )}
      </div>

      <div className="form-actions">
        <button
          type="button"
          className="button button--secondary"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancelar
        </button>
        <button type="submit" className="button button--primary" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando...' : isEditing ? 'Guardar cambios' : 'Crear compania'}
        </button>
      </div>
    </form>
  );
}
