import axios, { AxiosError, AxiosInstance } from 'axios';
import type { ProblemDetails } from '@/types/api';

/** Clave con la que se guarda el token de acceso en el navegador. */
export const ACCESS_TOKEN_STORAGE_KEY = 'sb.accessToken';

/** Clave con la que se guarda la sesion del usuario en el navegador. */
export const SESSION_STORAGE_KEY = 'sb.session';

/** Evento que se emite cuando la API rechaza el token por vencido o invalido. */
export const UNAUTHORIZED_EVENT_NAME = 'sb:unauthorized';

const REQUEST_TIMEOUT_IN_MILLISECONDS = 20_000;
const UNAUTHORIZED_STATUS_CODE = 401;

/**
 * Error de la API ya interpretado.
 *
 * Traducir el error una sola vez, aqui, evita que cada pantalla tenga que
 * conocer el formato ProblemDetails que devuelve el backend.
 */
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number | undefined,
    public readonly validationErrors: Record<string, string[]> | undefined,
    public readonly correlationId: string | undefined,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  /** Devuelve todos los mensajes de validacion en una lista plana. */
  get validationMessages(): string[] {
    if (!this.validationErrors) {
      return [];
    }

    return Object.values(this.validationErrors).flat();
  }
}

/**
 * Cliente HTTP compartido por toda la aplicacion.
 *
 * Un interceptor agrega el token de acceso a cada peticion y otro traduce los
 * errores y avisa cuando la sesion caduca. De esta forma ninguna pantalla
 * manipula encabezados de autorizacion.
 */
export const httpClient: AxiosInstance = axios.create({
  baseURL: '/api',
  timeout: REQUEST_TIMEOUT_IN_MILLISECONDS,
  headers: {
    'Content-Type': 'application/json',
  },
});

httpClient.interceptors.request.use((requestConfig) => {
  const accessToken = window.localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY);

  if (accessToken) {
    requestConfig.headers.Authorization = `Bearer ${accessToken}`;
  }

  return requestConfig;
});

httpClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ProblemDetails>) => {
    if (error.response?.status === UNAUTHORIZED_STATUS_CODE) {
      window.dispatchEvent(new Event(UNAUTHORIZED_EVENT_NAME));
    }

    return Promise.reject(translateError(error));
  },
);

function translateError(error: AxiosError<ProblemDetails>): ApiError {
  if (!error.response) {
    return new ApiError(
      'No se pudo comunicar con el servidor. Verifique que la API este en ejecucion.',
      undefined,
      undefined,
      undefined,
    );
  }

  const problemDetails = error.response.data;

  const message =
    problemDetails?.detail ??
    problemDetails?.title ??
    'Ocurrio un error al procesar la solicitud.';

  return new ApiError(
    message,
    error.response.status,
    problemDetails?.errors,
    problemDetails?.correlationId,
  );
}

/**
 * Elimina de un objeto de filtros los valores vacios, de modo que la API no
 * reciba parametros de consulta sin contenido.
 *
 * @param source Objeto con los filtros capturados en pantalla.
 * @returns Objeto listo para enviarse como parametros de consulta.
 */
export function buildQueryParameters(
  source: Record<string, unknown>,
): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(source).filter(
      ([, value]) => value !== undefined && value !== null && value !== '',
    ),
  );
}
