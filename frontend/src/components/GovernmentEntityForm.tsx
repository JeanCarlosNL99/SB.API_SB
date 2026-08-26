import { useState, type FormEvent } from 'react';
import { ErrorMessage } from './Feedback';
import type {
  GovernmentEntity,
  RecordStatus,
  UpdateGovernmentEntityRequest,
} from '@/types/api';

/** Valores capturados en el formulario de entidades gubernamentales. */
interface GovernmentEntityFormValues {
  name: string;
  category: string;
  stateBranch: string;
  sector: string;
  status: RecordStatus;
}

const MAXIMUM_NAME_LENGTH = 250;
const MAXIMUM_CLASSIFICATION_LENGTH = 150;

const EMPTY_VALUES: GovernmentEntityFormValues = {
  name: '',
  category: '',
  stateBranch: 'Poder Ejecutivo',
  sector: '',
  status: 'Active',
};

/**
 * Formulario de alta y edicion de entidades gubernamentales.
 *
 * El mismo componente atiende las dos operaciones: la pantalla de creacion lo
 * usa en linea y la consulta lo abre en una ventana modal para editar. Asi las
 * reglas de captura y sus mensajes existen una sola vez.
 */
export function GovernmentEntityForm({
  entity,
  catalogs,
  isSubmitting,
  submitError,
  onSubmit,
  onCancel,
}: {
  entity?: GovernmentEntity | null;
  catalogs: { categories: string[]; sectors: string[]; stateBranches: string[] };
  isSubmitting: boolean;
  submitError: unknown;
  onSubmit: (values: UpdateGovernmentEntityRequest) => Promise<void>;
  onCancel?: () => void;
}) {
  const [values, setValues] = useState<GovernmentEntityFormValues>(
    entity
      ? {
          name: entity.name,
          category: entity.category,
          stateBranch: entity.stateBranch,
          sector: entity.sector,
          status: entity.status,
        }
      : EMPTY_VALUES,
  );

  const [validationErrors, setValidationErrors] = useState<
    Partial<Record<keyof GovernmentEntityFormValues, string>>
  >({});

  function updateValue<TKey extends keyof GovernmentEntityFormValues>(
    key: TKey,
    value: GovernmentEntityFormValues[TKey],
  ) {
    setValues((previousValues) => ({ ...previousValues, [key]: value }));
    setValidationErrors((previousErrors) => ({ ...previousErrors, [key]: undefined }));
  }

  async function handleSubmit(formEvent: FormEvent<HTMLFormElement>) {
    formEvent.preventDefault();

    const errors = validate(values);

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);

      return;
    }

    await onSubmit({
      name: values.name.trim(),
      category: values.category.trim(),
      stateBranch: values.stateBranch.trim(),
      sector: values.sector.trim(),
      status: values.status,
    });
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <ErrorMessage error={submitError} />

      <div className="form-grid">
        <div className="field" style={{ gridColumn: '1 / -1' }}>
          <label className="field__label" htmlFor="entityName">
            Nombre de la entidad
          </label>
          <input
            id="entityName"
            className={`control${validationErrors.name ? ' control--invalid' : ''}`}
            type="text"
            maxLength={MAXIMUM_NAME_LENGTH}
            value={values.name}
            onChange={(changeEvent) => updateValue('name', changeEvent.target.value)}
          />
          {validationErrors.name && (
            <span className="field__error">{validationErrors.name}</span>
          )}
        </div>

        <SuggestedInput
          id="entityCategory"
          label="Categoria"
          suggestions={catalogs.categories}
          value={values.category}
          error={validationErrors.category}
          onChange={(value) => updateValue('category', value)}
        />

        <SuggestedInput
          id="entityStateBranch"
          label="Poder del Estado"
          suggestions={catalogs.stateBranches}
          value={values.stateBranch}
          error={validationErrors.stateBranch}
          onChange={(value) => updateValue('stateBranch', value)}
        />

        <SuggestedInput
          id="entitySector"
          label="Sector"
          suggestions={catalogs.sectors}
          value={values.sector}
          error={validationErrors.sector}
          onChange={(value) => updateValue('sector', value)}
        />

        {entity && (
          <div className="field">
            <label className="field__label" htmlFor="entityStatus">
              Estado
            </label>
            <select
              id="entityStatus"
              className="control"
              value={values.status}
              onChange={(changeEvent) =>
                updateValue('status', changeEvent.target.value as RecordStatus)
              }
            >
              <option value="Active">Activo</option>
              <option value="Inactive">Inactivo</option>
            </select>
          </div>
        )}
      </div>

      <div className="form-actions">
        {onCancel && (
          <button
            type="button"
            className="button button--secondary"
            onClick={onCancel}
            disabled={isSubmitting}
          >
            Cancelar
          </button>
        )}
        <button type="submit" className="button button--primary" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando...' : entity ? 'Guardar cambios' : 'Crear registro'}
        </button>
      </div>
    </form>
  );
}

/**
 * Campo de texto con sugerencias tomadas de los datos ya registrados. Permite
 * escribir un valor nuevo pero facilita reutilizar los existentes, lo que evita
 * que el mismo sector se guarde escrito de dos maneras distintas.
 */
function SuggestedInput({
  id,
  label,
  suggestions,
  value,
  error,
  onChange,
}: {
  id: string;
  label: string;
  suggestions: string[];
  value: string;
  error?: string;
  onChange: (value: string) => void;
}) {
  const listId = `${id}-sugerencias`;

  return (
    <div className="field">
      <label className="field__label" htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        className={`control${error ? ' control--invalid' : ''}`}
        type="text"
        list={listId}
        maxLength={MAXIMUM_CLASSIFICATION_LENGTH}
        value={value}
        onChange={(changeEvent) => onChange(changeEvent.target.value)}
      />
      <datalist id={listId}>
        {suggestions.map((suggestion) => (
          <option key={suggestion} value={suggestion} />
        ))}
      </datalist>
      {error && <span className="field__error">{error}</span>}
    </div>
  );
}

function validate(
  values: GovernmentEntityFormValues,
): Partial<Record<keyof GovernmentEntityFormValues, string>> {
  const errors: Partial<Record<keyof GovernmentEntityFormValues, string>> = {};

  if (values.name.trim().length === 0) {
    errors.name = 'El nombre de la entidad es obligatorio.';
  }

  if (values.category.trim().length === 0) {
    errors.category = 'La categoria es obligatoria.';
  }

  if (values.stateBranch.trim().length === 0) {
    errors.stateBranch = 'El poder del Estado es obligatorio.';
  }

  if (values.sector.trim().length === 0) {
    errors.sector = 'El sector es obligatorio.';
  }

  return errors;
}
