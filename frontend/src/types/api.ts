/**
 * Contratos de la API tipados en TypeScript.
 *
 * Se declaran aqui una sola vez y todas las pantallas los reutilizan: si un
 * campo cambia en el backend, el compilador senala cada lugar del cliente que
 * hay que ajustar en lugar de fallar en tiempo de ejecucion.
 */

/** Estado de un registro de mantenimiento. */
export type RecordStatus = 'Active' | 'Inactive';

/** Estado laboral de un empleado. */
export type EmployeeStatus = 'Active' | 'Inactive';

/** Tipo de contrato de un empleado. */
export type EmployeeType =
  | 'Salaried'
  | 'Hourly'
  | 'Commission'
  | 'BaseSalariedCommission';

/** Respuesta paginada estandar de la API. */
export interface PagedResponse<TItem> {
  items: TItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** Credenciales de inicio de sesion. */
export interface LoginRequest {
  userName: string;
  password: string;
}

/** Resultado de una autenticacion exitosa. */
export interface AuthenticationResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  userId: string;
  userName: string;
  fullName: string;
  roles: string[];
}

/** Entidad gubernamental devuelta por la API. */
export interface GovernmentEntity {
  id: string;
  name: string;
  category: string;
  stateBranch: string;
  sector: string;
  status: RecordStatus;
  statusDescription: string;
  createdAt: string;
  updatedAt?: string | null;
}

/**
 * Entidad gubernamental reducida a lo que necesita un selector.
 *
 * La consulta paginada no sirve para llenar un selector: recortaria el listado en
 * silencio al superar el tamano maximo de pagina. La API expone un endpoint
 * propio que devuelve el listado completo con solo estos dos campos.
 */
export interface GovernmentEntityOption {
  id: string;
  name: string;
}

/** Filtros de la consulta de entidades gubernamentales. */
export interface GovernmentEntityFilter {
  name?: string;
  category?: string;
  sector?: string;
  stateBranch?: string;
  status?: RecordStatus | '';
  pageNumber: number;
  pageSize: number;
}

/** Catalogos que alimentan los filtros de entidades gubernamentales. */
export interface GovernmentEntityCatalogs {
  categories: string[];
  sectors: string[];
  stateBranches: string[];
}

/** Departamento organizacional. */
export interface Department {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  employeeCount: number;
}

/** Componente individual del calculo de pago. */
export interface PaymentComponent {
  concept: string;
  detail: string;
  amount: number;
}

/** Desglose del calculo de pago de un empleado. */
export interface PaymentBreakdown {
  formula: string;
  components: PaymentComponent[];
  totalAmount: number;
}

/** Empleado devuelto por la API. */
export interface Employee {
  id: string;
  firstName?: string | null;
  paternalLastName: string;
  fullName: string;
  socialSecurityNumber: string;
  type: EmployeeType;
  typeDescription: string;
  status: EmployeeStatus;
  statusDescription: string;
  governmentEntityId: string;
  governmentEntityName: string;
  departmentId: string;
  departmentName: string;
  weeklySalary?: number | null;
  hourlyWage?: number | null;
  hoursWorked?: number | null;
  grossSales?: number | null;
  commissionRate?: number | null;
  baseSalary?: number | null;
  weeklyPayment: number;
  paymentBreakdown?: PaymentBreakdown | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Datos de captura de un empleado, comunes al alta y a la actualizacion. */
export interface EmployeeRequest {
  type: EmployeeType;
  firstName?: string | null;
  paternalLastName: string;
  socialSecurityNumber: string;
  governmentEntityId: string;
  departmentId: string;
  status: EmployeeStatus;
  weeklySalary?: number | null;
  hourlyWage?: number | null;
  hoursWorked?: number | null;
  grossSales?: number | null;
  commissionRate?: number | null;
  baseSalary?: number | null;
}

/** Filtros de la consulta de empleados. */
export interface EmployeeFilter {
  name?: string;
  governmentEntityId?: string;
  departmentId?: string;
  status?: EmployeeStatus | '';
  type?: EmployeeType | '';
  pageNumber: number;
  pageSize: number;
}

/** Total agregado de nomina para un agrupamiento determinado. */
export interface PayrollSummaryItem {
  groupName: string;
  employeeCount: number;
  totalWeeklyPayment: number;
}

/** Rol de seguridad. */
export interface Role {
  id: string;
  name: string;
  description: string;
}

/** Usuario del sistema. */
export interface User {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  isActive: boolean;
  lastLoginAt?: string | null;
  createdAt: string;
  roles: Role[];
}

/** Datos para crear un usuario. */
export interface CreateUserRequest {
  userName: string;
  email: string;
  fullName: string;
  password: string;
  roleIdentifiers: string[];
}

/** Datos para actualizar un usuario. */
export interface UpdateUserRequest {
  email: string;
  fullName: string;
  isActive: boolean;
  roleIdentifiers: string[];
}

/** Respuesta de error de la API, en formato ProblemDetails. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errorCode?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}

/* ------------------------------------------------------------------ */
/* Calculo de pagos semanales por entidad gubernamental                */
/* ------------------------------------------------------------------ */

/** Estado de una ejecucion de nomina. */
export type PayrollRunStatus = 'Generated' | 'Cancelled';

/** Datos para generar la nomina de una semana. */
export interface GeneratePayrollRunRequest {
  governmentEntityId: string;
  year: number;
  weekNumber: number;
  onlyActiveEmployees: boolean;
}

/** Motivo con el que se anula una ejecucion de nomina. */
export interface CancelPayrollRunRequest {
  reason: string;
}

/** Filtros del historial de nomina. */
export interface PayrollRunFilter {
  governmentEntityId?: string;
  year?: number | '';
  includeCancelled: boolean;
  pageNumber: number;
  pageSize: number;
}

/** Cabecera de una ejecucion de nomina. */
export interface PayrollRunSummary {
  id: string;
  governmentEntityId: string;
  governmentEntityName: string;
  year: number;
  weekNumber: number;
  weekLabel: string;
  weekStartDate: string;
  weekEndDate: string;
  status: PayrollRunStatus;
  statusDescription: string;
  employeeCount: number;
  totalAmount: number;
  generatedAt: string;
  generatedBy: string;
  cancellationReason?: string | null;
  cancelledAt?: string | null;
}

/** Linea de una ejecucion de nomina. */
export interface PayrollRunLine {
  id: string;
  employeeId?: string | null;
  employeeFullName: string;
  socialSecurityNumber: string;
  employeeType: EmployeeType;
  employeeTypeDescription: string;
  departmentName: string;
  weeklyPayment: number;
  paymentFormula: string;
  components: PaymentComponent[];
}

/** Ejecucion de nomina con su detalle completo. */
export interface PayrollRunDetail {
  summary: PayrollRunSummary;
  lines: PayrollRunLine[];
  totalsByType: PayrollSummaryItem[];
  totalsByDepartment: PayrollSummaryItem[];
}

/** Vista previa del calculo de una semana antes de generarla. */
export interface PayrollPreview {
  governmentEntityId: string;
  governmentEntityName: string;
  year: number;
  weekNumber: number;
  weekLabel: string;
  weekStartDate: string;
  weekEndDate: string;
  employeeCount: number;
  totalAmount: number;
  isAlreadyGenerated: boolean;
  existingPayrollRunId?: string | null;
  lines: PayrollRunLine[];
  totalsByType: PayrollSummaryItem[];
  totalsByDepartment: PayrollSummaryItem[];
}

/**
 * Entidad gubernamental con empleados registrados, es decir, con nomina que
 * calcular. El selector de la pantalla de calculo muestra estas y no las 181 del
 * listado oficial: ofrecer una entidad sin empleados solo conduce a un calculo
 * vacio que la API rechaza.
 */
export interface PayableGovernmentEntity {
  id: string;
  name: string;
  totalEmployeeCount: number;
  activeEmployeeCount: number;
}

/** Semanas ya pagadas por una entidad gubernamental en un ano. */
export interface GeneratedWeeks {
  governmentEntityId: string;
  year: number;
  weeksInYear: number;
  generatedWeekNumbers: number[];
}

/* ------------------------------------------------------------------ */
/* Registro de eventos                                                 */
/* ------------------------------------------------------------------ */

/** Nivel de un evento registrado. */
export type EventLogLevel =
  | 'Verbose'
  | 'Debug'
  | 'Information'
  | 'Warning'
  | 'Error'
  | 'Fatal';

/** Archivo de registro disponible para consulta. */
export interface EventLogFile {
  fileName: string;
  sizeInBytes: number;
  lastWriteAtUtc: string;
}

/** Filtros de la consulta del registro de eventos. */
export interface EventLogFilter {
  fileName?: string;
  minimumLevel?: EventLogLevel | '';
  searchTerm?: string;
  maximumEntries: number;
}

/** Entrada individual del registro de eventos. */
export interface EventLogEntry {
  timestamp: string;
  level: EventLogLevel;
  message: string;
  correlationId?: string | null;
  userName?: string | null;
  sourceContext?: string | null;
  exception?: string | null;
}

/** Resultado de la consulta del registro de eventos. */
export interface EventLogResult {
  fileName: string;
  entryCount: number;
  hasMoreEntries: boolean;
  countsByLevel: Record<string, number>;
  entries: EventLogEntry[];
}
