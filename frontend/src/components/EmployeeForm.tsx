import { useMemo, useState, type FormEvent } from 'react';
import {
  EMPLOYEE_TYPE_DEFINITIONS,
  type EmployeeTypeDefinition,
} from '@/constants/employeeTypes';
import { ErrorMessage } from './Feedback';
import type {
  Department,
  Employee,
  EmployeeRequest,
  EmployeeStatus,
  EmployeeType,
  GovernmentEntityOption,
} from '@/types/api';

const SOCIAL_SECURITY_NUMBER_PATTERN = /^[0-9A-Za-z-]+$/;
const SOCIAL_SECURITY_NUMBER_MINIMUM_LENGTH = 5;

type FormValues = Record<string, string> & {
  type: EmployeeType;
  status: EmployeeStatus;
};

/**
 * Formulario de captura de empleados con campos dependientes del tipo.
 *
 * El pago semanal no se calcula aqui a proposito: la formula vive unicamente en
 * el dominio del backend. Duplicarla en el cliente crearia dos versiones de la
 * misma regla que podrian discrepar. La interfaz muestra la formula como texto y
 * el monto calculado que devuelve la API.
 */
export function EmployeeForm({
  employee,
  governmentEntities,
  departments,
  isSubmitting,
  submitError,
  onSubmit,
  onCancel,
}: {
  employee?: Employee | null;
  governmentEntities: GovernmentEntityOption[];
  departments: Department[];
  isSubmitting: boolean;
  submitError: unknown;
  onSubmit: (request: EmployeeRequest) => Promise<void>;
  onCancel: () => void;
}) {
  const isEditing = employee !== null && employee !== undefined;

  const [values, setValues] = useState<FormValues>(() => buildInitialValues(employee));
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  const typeDefinition = useMemo(
    () =>
      EMPLOYEE_TYPE_DEFINITIONS.find((definition) => definition.value === values.type) ??
      EMPLOYEE_TYPE_DEFINITIONS[0],
    [values.type],
  );

  const activeDepartments = useMemo(
    () => departments.filter((department) => department.isActive),
    [departments],
  );

  function updateValue(key: string, value: string) {
    setValues((previousValues) => ({ ...previousValues, [key]: value }));
    setValidationErrors((previousErrors) => ({ ...previousErrors, [key]: '' }));
  }

  async function handleSubmit(formEvent: FormEvent<HTMLFormElement>) {
    formEvent.preventDefault();

    const errors = validate(values, typeDefinition);

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);

      return;
    }

    await onSubmit(buildRequest(values, typeDefinition));
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <ErrorMessage error={submitError} />

      <div className="form-grid">
        <div className="field">
          <label className="field__label" htmlFor="employeeType">
            Tipo de empleado
          </label>
          <select
            id="employeeType"
            className="control"
            value={values.type}
            disabled={isEditing}
            onChange={(changeEvent) => {
              // Al cambiar de tipo se limpian los campos numericos del tipo
              // anterior para no enviar datos que no corresponden.
              setValidationErrors({});
              setValues((previousValues) => ({
                ...buildEmptyNumericValues(),
                type: changeEvent.target.value as EmployeeType,
                status: previousValues.status,
                firstName: previousValues.firstName,
                paternalLastName: previousValues.paternalLastName,
                socialSecurityNumber: previousValues.socialSecurityNumber,
                governmentEntityId: previousValues.governmentEntityId,
                departmentId: previousValues.departmentId,
              }));
            }}
          >
            {EMPLOYEE_TYPE_DEFINITIONS.map((definition) => (
              <option key={definition.value} value={definition.value}>
                {definition.label}
              </option>
            ))}
          </select>
          {isEditing && (
            <span className="field__hint">
              El tipo de contrato no se puede cambiar en un empleado ya registrado.
            </span>
          )}
        </div>

        <div className="field">
          <label className="field__label" htmlFor="employeeStatus">
            Estado
          </label>
          <select
            id="employeeStatus"
            className="control"
            value={values.status}
            onChange={(changeEvent) => updateValue('status', changeEvent.target.value)}
          >
            <option value="Active">Activo</option>
            <option value="Inactive">Inactivo</option>
          </select>
        </div>

        <TextField
          id="firstName"
          label={
            typeDefinition.requiresFirstName ? 'Primer nombre' : 'Primer nombre (opcional)'
          }
          value={values.firstName}
          error={validationErrors.firstName}
          onChange={(value) => updateValue('firstName', value)}
        />

        <TextField
          id="paternalLastName"
          label="Apellido paterno"
          value={values.paternalLastName}
          error={validationErrors.paternalLastName}
          onChange={(value) => updateValue('paternalLastName', value)}
        />

        <TextField
          id="socialSecurityNumber"
          label="Numero de seguro social"
          hint="Solo letras, numeros y guiones. Ejemplo: 001-0000001-1"
          value={values.socialSecurityNumber}
          error={validationErrors.socialSecurityNumber}
          onChange={(value) => updateValue('socialSecurityNumber', value)}
        />

        <div className="field">
          <label className="field__label" htmlFor="governmentEntityId">
            Entidad gubernamental
          </label>
          <select
            id="governmentEntityId"
            className={`control${validationErrors.governmentEntityId ? ' control--invalid' : ''}`}
            value={values.governmentEntityId}
            onChange={(changeEvent) => updateValue('governmentEntityId', changeEvent.target.value)}
          >
            <option value="">Seleccione una entidad gubernamental</option>
            {governmentEntities.map((governmentEntity) => (
              <option key={governmentEntity.id} value={governmentEntity.id}>
                {governmentEntity.name}
              </option>
            ))}
          </select>
          <span className="field__hint">
            El pago semanal del empleado se calcula con la nomina de su entidad.
          </span>
          {validationErrors.governmentEntityId && (
            <span className="field__error">{validationErrors.governmentEntityId}</span>
          )}
        </div>

        <div className="field">
          <label className="field__label" htmlFor="departmentId">
            Departamento
          </label>
          <select
            id="departmentId"
            className={`control${validationErrors.departmentId ? ' control--invalid' : ''}`}
            value={values.departmentId}
            onChange={(changeEvent) => updateValue('departmentId', changeEvent.target.value)}
          >
            <option value="">Seleccione un departamento</option>
            {activeDepartments.map((department) => (
              <option key={department.id} value={department.id}>
                {department.name} ({department.code})
              </option>
            ))}
          </select>
          {validationErrors.departmentId && (
            <span className="field__error">{validationErrors.departmentId}</span>
          )}
        </div>

        {typeDefinition.numericFields.map((numericField) => (
          <div className="field" key={String(numericField.key)}>
            <label className="field__label" htmlFor={String(numericField.key)}>
              {numericField.label}
            </label>
            <input
              id={String(numericField.key)}
              className={`control${
                validationErrors[String(numericField.key)] ? ' control--invalid' : ''
              }`}
              type="number"
              inputMode="decimal"
              step={numericField.step}
              min={numericField.minimumValue}
              max={numericField.maximumValue}
              value={values[String(numericField.key)] ?? ''}
              onChange={(changeEvent) =>
                updateValue(String(numericField.key), changeEvent.target.value)
              }
            />
            <span className="field__hint">{numericField.hint}</span>
            {validationErrors[String(numericField.key)] && (
              <span className="field__error">
                {validationErrors[String(numericField.key)]}
              </span>
            )}
          </div>
        ))}
      </div>

      <div style={{ marginTop: 16 }}>
        <p className="section-title">Formula aplicada por el sistema</p>
        <code className="formula">{typeDefinition.formula}</code>
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
          {isSubmitting ? 'Guardando...' : isEditing ? 'Guardar cambios' : 'Crear empleado'}
        </button>
      </div>
    </form>
  );
}

function TextField({
  id,
  label,
  hint,
  value,
  error,
  onChange,
}: {
  id: string;
  label: string;
  hint?: string;
  value: string;
  error?: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="field">
      <label className="field__label" htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        className={`control${error ? ' control--invalid' : ''}`}
        type="text"
        value={value}
        onChange={(changeEvent) => onChange(changeEvent.target.value)}
      />
      {hint && <span className="field__hint">{hint}</span>}
      {error && <span className="field__error">{error}</span>}
    </div>
  );
}

function buildEmptyNumericValues(): Record<string, string> {
  return {
    weeklySalary: '',
    hourlyWage: '',
    hoursWorked: '',
    grossSales: '',
    commissionRate: '',
    baseSalary: '',
  };
}

function buildInitialValues(employee: Employee | null | undefined): FormValues {
  return {
    ...buildEmptyNumericValues(),
    type: employee?.type ?? 'Salaried',
    status: employee?.status ?? 'Active',
    firstName: employee?.firstName ?? '',
    governmentEntityId: employee?.governmentEntityId ?? '',
    paternalLastName: employee?.paternalLastName ?? '',
    socialSecurityNumber: employee?.socialSecurityNumber ?? '',
    departmentId: employee?.departmentId ?? '',
    weeklySalary: numberToInput(employee?.weeklySalary),
    hourlyWage: numberToInput(employee?.hourlyWage),
    hoursWorked: numberToInput(employee?.hoursWorked),
    grossSales: numberToInput(employee?.grossSales),
    commissionRate: numberToInput(employee?.commissionRate),
    baseSalary: numberToInput(employee?.baseSalary),
  };
}

function numberToInput(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value);
}

function validate(
  values: FormValues,
  typeDefinition: EmployeeTypeDefinition,
): Record<string, string> {
  const errors: Record<string, string> = {};

  if (typeDefinition.requiresFirstName && values.firstName.trim().length === 0) {
    errors.firstName = 'El primer nombre es obligatorio para este tipo de empleado.';
  }

  if (values.paternalLastName.trim().length === 0) {
    errors.paternalLastName = 'El apellido paterno es obligatorio.';
  }

  const socialSecurityNumber = values.socialSecurityNumber.trim();

  if (socialSecurityNumber.length < SOCIAL_SECURITY_NUMBER_MINIMUM_LENGTH) {
    errors.socialSecurityNumber = `Debe tener al menos ${SOCIAL_SECURITY_NUMBER_MINIMUM_LENGTH} caracteres.`;
  } else if (!SOCIAL_SECURITY_NUMBER_PATTERN.test(socialSecurityNumber)) {
    errors.socialSecurityNumber = 'Solo admite letras, numeros y guiones.';
  }

  if (values.governmentEntityId.length === 0) {
    errors.governmentEntityId = 'Seleccione una entidad gubernamental.';
  }

  if (values.departmentId.length === 0) {
    errors.departmentId = 'Seleccione un departamento.';
  }

  for (const numericField of typeDefinition.numericFields) {
    const rawValue = values[String(numericField.key)];

    if (rawValue === undefined || rawValue.trim().length === 0) {
      errors[String(numericField.key)] = `${numericField.label} es obligatorio.`;
      continue;
    }

    const parsedValue = Number(rawValue);

    if (Number.isNaN(parsedValue)) {
      errors[String(numericField.key)] = 'Debe capturar un valor numerico.';
      continue;
    }

    if (parsedValue < numericField.minimumValue) {
      errors[String(numericField.key)] =
        `El valor minimo permitido es ${numericField.minimumValue}.`;
      continue;
    }

    if (numericField.maximumValue !== undefined && parsedValue > numericField.maximumValue) {
      errors[String(numericField.key)] =
        `El valor maximo permitido es ${numericField.maximumValue}.`;
    }
  }

  return errors;
}

function buildRequest(
  values: FormValues,
  typeDefinition: EmployeeTypeDefinition,
): EmployeeRequest {
  const request: EmployeeRequest = {
    type: typeDefinition.value,
    firstName: values.firstName.trim().length > 0 ? values.firstName.trim() : null,
    paternalLastName: values.paternalLastName.trim(),
    socialSecurityNumber: values.socialSecurityNumber.trim(),
    governmentEntityId: values.governmentEntityId,
    departmentId: values.departmentId,
    status: values.status,
  };

  // Solo se envian los campos numericos que corresponden al tipo seleccionado.
  for (const numericField of typeDefinition.numericFields) {
    const parsedValue = Number(values[String(numericField.key)]);

    Object.assign(request, { [numericField.key]: parsedValue });
  }

  return request;
}
