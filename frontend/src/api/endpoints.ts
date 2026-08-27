import { buildQueryParameters, httpClient } from './httpClient';
import type {
  AuthenticationResponse,
  CreateGovernmentEntityRequest,
  CreateUserRequest,
  Department,
  Employee,
  EmployeeFilter,
  EmployeeRequest,
  GovernmentEntity,
  GovernmentEntityCatalogs,
  GovernmentEntityFilter,
  LoginRequest,
  PagedResponse,
  Role,
  UpdateGovernmentEntityRequest,
  UpdateUserRequest,
  User,
} from '@/types/api';

/**
 * Funciones de acceso a la API, agrupadas por recurso.
 *
 * Concentrar las rutas en un unico modulo evita cadenas de URL dispersas por los
 * componentes y facilita cambiar un endpoint sin buscarlo en toda la aplicacion.
 */

export const authenticationApi = {
  /** Inicia sesion y obtiene el token de acceso. */
  async login(request: LoginRequest): Promise<AuthenticationResponse> {
    const { data } = await httpClient.post<AuthenticationResponse>(
      '/autenticacion/iniciar-sesion',
      request,
    );

    return data;
  },
};

export const governmentEntitiesApi = {
  /** Consulta entidades gubernamentales con filtros y paginacion. */
  async search(filter: GovernmentEntityFilter): Promise<PagedResponse<GovernmentEntity>> {
    const { data } = await httpClient.get<PagedResponse<GovernmentEntity>>(
      '/entidades-gubernamentales',
      { params: buildQueryParameters({ ...filter }) },
    );

    return data;
  },

  /** Obtiene los catalogos de categoria, sector y poder del Estado. */
  async getCatalogs(): Promise<GovernmentEntityCatalogs> {
    const { data } = await httpClient.get<GovernmentEntityCatalogs>(
      '/entidades-gubernamentales/catalogos',
    );

    return data;
  },

  /** Registra una nueva entidad gubernamental. */
  async create(request: CreateGovernmentEntityRequest): Promise<GovernmentEntity> {
    const { data } = await httpClient.post<GovernmentEntity>(
      '/entidades-gubernamentales',
      request,
    );

    return data;
  },

  /** Actualiza una entidad gubernamental existente. */
  async update(
    entityId: string,
    request: UpdateGovernmentEntityRequest,
  ): Promise<GovernmentEntity> {
    const { data } = await httpClient.put<GovernmentEntity>(
      `/entidades-gubernamentales/${entityId}`,
      request,
    );

    return data;
  },

  /** Elimina una entidad gubernamental. */
  async remove(entityId: string): Promise<void> {
    await httpClient.delete(`/entidades-gubernamentales/${entityId}`);
  },
};

export const employeesApi = {
  /** Consulta empleados con filtros por nombre, departamento, estado y tipo. */
  async search(filter: EmployeeFilter): Promise<PagedResponse<Employee>> {
    const { data } = await httpClient.get<PagedResponse<Employee>>('/empleados', {
      params: buildQueryParameters({ ...filter }),
    });

    return data;
  },

  /** Obtiene un empleado con el desglose de su calculo de pago. */
  async getById(employeeId: string): Promise<Employee> {
    const { data } = await httpClient.get<Employee>(`/empleados/${employeeId}`);

    return data;
  },

  /** Registra un nuevo empleado. */
  async create(request: EmployeeRequest): Promise<Employee> {
    const { data } = await httpClient.post<Employee>('/empleados', request);

    return data;
  },

  /** Actualiza un empleado y recalcula su pago semanal. */
  async update(employeeId: string, request: EmployeeRequest): Promise<Employee> {
    const { data } = await httpClient.put<Employee>(`/empleados/${employeeId}`, request);

    return data;
  },

  /** Elimina un empleado. */
  async remove(employeeId: string): Promise<void> {
    await httpClient.delete(`/empleados/${employeeId}`);
  },
};

export const departmentsApi = {
  /** Obtiene todos los departamentos. */
  async getAll(): Promise<Department[]> {
    const { data } = await httpClient.get<Department[]>('/departamentos');

    return data;
  },
};

export const usersApi = {
  /** Obtiene todos los usuarios con sus roles. */
  async getAll(): Promise<User[]> {
    const { data } = await httpClient.get<User[]>('/usuarios');

    return data;
  },

  /** Obtiene los roles disponibles. */
  async getRoles(): Promise<Role[]> {
    const { data } = await httpClient.get<Role[]>('/usuarios/roles');

    return data;
  },

  /** Registra un nuevo usuario. */
  async create(request: CreateUserRequest): Promise<User> {
    const { data } = await httpClient.post<User>('/usuarios', request);

    return data;
  },

  /** Actualiza los datos y roles de un usuario. */
  async update(userId: string, request: UpdateUserRequest): Promise<User> {
    const { data } = await httpClient.put<User>(`/usuarios/${userId}`, request);

    return data;
  },

  /** Elimina un usuario. */
  async remove(userId: string): Promise<void> {
    await httpClient.delete(`/usuarios/${userId}`);
  },
};
