import type { EmployeeRequest, EmployeeType } from '@/types/api';

/** Campo numerico especifico de un tipo de empleado. */
export interface NumericFieldDefinition {
  key: keyof EmployeeRequest;
  label: string;
  hint: string;
  step: string;
  minimumValue: number;
  maximumValue?: number;
}

/** Definicion completa de un tipo de empleado en la interfaz. */
export interface EmployeeTypeDefinition {
  value: EmployeeType;
  label: string;
  formula: string;
  requiresFirstName: boolean;
  numericFields: NumericFieldDefinition[];
}

const GROSS_SALES_FIELD: NumericFieldDefinition = {
  key: 'grossSales',
  label: 'Ventas brutas',
  hint: 'Total vendido en la semana.',
  step: '0.01',
  minimumValue: 0,
};

const COMMISSION_RATE_FIELD: NumericFieldDefinition = {
  key: 'commissionRate',
  label: 'Tarifa de comision',
  hint: 'Fraccion decimal. Ejemplo: 0.08 equivale a 8%.',
  step: '0.0001',
  minimumValue: 0.0001,
  maximumValue: 1,
};

/**
 * Catalogo de tipos de empleado declarado como datos.
 *
 * Es el reflejo en la interfaz del diseno del backend: agregar un quinto tipo de
 * contrato consiste en anadir una entrada aqui, sin modificar el formulario, los
 * filtros ni la pantalla que los usa.
 */
export const EMPLOYEE_TYPE_DEFINITIONS: EmployeeTypeDefinition[] = [
  {
    value: 'Salaried',
    label: 'Empleado asalariado',
    formula: 'pagoSemanal = salarioSemanal',
    requiresFirstName: true,
    numericFields: [
      {
        key: 'weeklySalary',
        label: 'Salario semanal',
        hint: 'Monto fijo que recibe cada semana.',
        step: '0.01',
        minimumValue: 0.01,
      },
    ],
  },
  {
    value: 'Hourly',
    label: 'Empleado por horas',
    formula:
      'pagoSemanal = sueldoPorHora x horas (hasta 40) + sueldoPorHora x 1.5 x horas extras',
    requiresFirstName: false,
    numericFields: [
      {
        key: 'hourlyWage',
        label: 'Sueldo por hora',
        hint: 'Valor pactado por hora trabajada.',
        step: '0.01',
        minimumValue: 0.01,
      },
      {
        key: 'hoursWorked',
        label: 'Horas trabajadas',
        hint: 'Las horas por encima de 40 se pagan con recargo de 1.5.',
        step: '0.01',
        minimumValue: 0,
        maximumValue: 168,
      },
    ],
  },
  {
    value: 'Commission',
    label: 'Empleado por comision',
    formula: 'pagoSemanal = ventasBrutas x tarifaComision',
    requiresFirstName: true,
    numericFields: [GROSS_SALES_FIELD, COMMISSION_RATE_FIELD],
  },
  {
    value: 'BaseSalariedCommission',
    label: 'Empleado asalariado por comision',
    formula:
      'pagoSemanal = (ventasBrutas x tarifaComision) + salarioBase + (salarioBase x 0.10)',
    requiresFirstName: true,
    numericFields: [
      GROSS_SALES_FIELD,
      COMMISSION_RATE_FIELD,
      {
        key: 'baseSalary',
        label: 'Salario base',
        hint: 'Recibe un incentivo adicional del 10%.',
        step: '0.01',
        minimumValue: 0.01,
      },
    ],
  },
];
