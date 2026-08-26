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

/** Datos para crear una entidad gubernamental. */
export interface CreateGovernmentEntityRequest {
  name: string;
  category: string;
  stateBranch: string;
  sector: string;
}

/** Datos para actualizar una entidad gubernamental. */
export interface UpdateGovernmentEntityRequest extends CreateGovernmentEntityRequest {
  status: RecordStatus;
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
  departmentId?: string;
  status?: EmployeeStatus | '';
  type?: EmployeeType | '';
  pageNumber: number;
  pageSize: number;
}

/** Linea del reporte semanal de nomina. */
export interface PayrollReportLine {
  employeeId: string;
  fullName: string;
  socialSecurityNumber: string;
  type: EmployeeType;
  typeDescription: string;
  departmentName: string;
  status: EmployeeStatus;
  weeklyPayment: number;
  paymentBreakdown: PaymentBreakdown;
}

/** Total agregado del reporte de nomina. */
export interface PayrollSummaryItem {
  groupName: string;
  employeeCount: number;
  totalWeeklyPayment: number;
}

/** Reporte semanal de nomina. */
export interface WeeklyPayrollReport {
  generatedAtUtc: string;
  onlyActiveEmployees: boolean;
  employeeCount: number;
  totalWeeklyPayment: number;
  lines: PayrollReportLine[];
  totalsByType: PayrollSummaryItem[];
  totalsByDepartment: PayrollSummaryItem[];
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
