import { buildQueryParameters, httpClient } from './httpClient';
import type {
  CancelPayrollRunRequest,
  EventLogFile,
  EventLogFilter,
  EventLogResult,
  GeneratePayrollRunRequest,
  GeneratedWeeks,
  PagedResponse,
  PayableGovernmentEntity,
  PayrollPreview,
  PayrollRunDetail,
  PayrollRunFilter,
  PayrollRunSummary,
} from '@/types/api';

/**
 * Acceso a la API de calculo de pagos semanales y registro de eventos. Se separa
 * del modulo de endpoints original para que cada archivo agrupe un conjunto
 * coherente de recursos.
 */

export const payrollApi = {
  /**
   * Obtiene las entidades gubernamentales que tienen empleados y por tanto
   * nomina que calcular.
   */
  async getPayableEntities(): Promise<PayableGovernmentEntity[]> {
    const { data } = await httpClient.get<PayableGovernmentEntity[]>(
      '/nomina/entidades',
    );

    return data;
  },

  /**
   * Calcula la nomina de una semana sin almacenarla. Informa si la semana ya
   * fue pagada, de modo que la pantalla pueda impedir la generacion antes de
   * intentarla.
   */
  async getPreview(
    governmentEntityId: string,
    year: number,
    weekNumber: number,
    onlyActiveEmployees: boolean,
  ): Promise<PayrollPreview> {
    const { data } = await httpClient.get<PayrollPreview>('/nomina/vista-previa', {
      params: { governmentEntityId, year, weekNumber, onlyActiveEmployees },
    });

    return data;
  },

  /** Genera y almacena la nomina de una semana. */
  async generate(request: GeneratePayrollRunRequest): Promise<PayrollRunDetail> {
    const { data } = await httpClient.post<PayrollRunDetail>('/nomina/ejecuciones', request);

    return data;
  },

  /** Consulta el historial de nominas generadas. */
  async searchHistory(filter: PayrollRunFilter): Promise<PagedResponse<PayrollRunSummary>> {
    const { data } = await httpClient.get<PagedResponse<PayrollRunSummary>>(
      '/nomina/ejecuciones',
      { params: buildQueryParameters({ ...filter }) },
    );

    return data;
  },

  /** Obtiene una nomina generada con su detalle completo. */
  async getById(payrollRunId: string): Promise<PayrollRunDetail> {
    const { data } = await httpClient.get<PayrollRunDetail>(
      `/nomina/ejecuciones/${payrollRunId}`,
    );

    return data;
  },

  /** Anula una nomina generada, liberando la semana para recalcularla. */
  async cancel(
    payrollRunId: string,
    request: CancelPayrollRunRequest,
  ): Promise<PayrollRunDetail> {
    const { data } = await httpClient.post<PayrollRunDetail>(
      `/nomina/ejecuciones/${payrollRunId}/anular`,
      request,
    );

    return data;
  },

  /** Obtiene las semanas de un ano que ya tienen nomina generada. */
  async getGeneratedWeeks(governmentEntityId: string, year: number): Promise<GeneratedWeeks> {
    const { data } = await httpClient.get<GeneratedWeeks>('/nomina/semanas-generadas', {
      params: { governmentEntityId, year },
    });

    return data;
  },
};

export const eventLogApi = {
  /** Obtiene los archivos de registro disponibles. */
  async getFiles(): Promise<EventLogFile[]> {
    const { data } = await httpClient.get<EventLogFile[]>('/registro-eventos/archivos');

    return data;
  },

  /** Lee las entradas del registro aplicando los filtros indicados. */
  async read(filter: EventLogFilter): Promise<EventLogResult> {
    const { data } = await httpClient.get<EventLogResult>('/registro-eventos', {
      params: buildQueryParameters({ ...filter }),
    });

    return data;
  },
};
