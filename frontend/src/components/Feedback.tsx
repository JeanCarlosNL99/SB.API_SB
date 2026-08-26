import type { ReactNode } from 'react';
import { ApiError } from '@/api/httpClient';

/**
 * Componentes de retroalimentacion al usuario: errores, exito, carga y estados
 * vacios. Se centralizan para que todas las pantallas comuniquen lo mismo de la
 * misma forma.
 */

/** Muestra un error de la API, incluidos los mensajes de validacion por campo. */
export function ErrorMessage({ error }: { error: unknown }) {
  if (!error) {
    return null;
  }

  const message =
    error instanceof Error ? error.message : 'Ocurrio un error inesperado.';

  const validationMessages =
    error instanceof ApiError ? error.validationMessages : [];

  return (
    <div className="alert alert--error" role="alert">
      <div>
        <strong>{message}</strong>
        {validationMessages.length > 0 && (
          <ul className="alert__list">
            {validationMessages.map((validationMessage) => (
              <li key={validationMessage}>{validationMessage}</li>
            ))}
          </ul>
        )}
        {error instanceof ApiError && error.correlationId && (
          <p className="field__hint">Referencia: {error.correlationId}</p>
        )}
      </div>
    </div>
  );
}

/** Muestra un mensaje de operacion exitosa. */
export function SuccessMessage({ message }: { message: string | null }) {
  if (!message) {
    return null;
  }

  return (
    <div className="alert alert--success" role="status">
      {message}
    </div>
  );
}

/** Indicador de carga con texto descriptivo. */
export function LoadingIndicator({ label = 'Cargando informacion...' }: { label?: string }) {
  return (
    <div className="loading-indicator" role="status">
      <span className="spinner" />
      {label}
    </div>
  );
}

/** Estado vacio, con un titulo y una accion opcional. */
export function EmptyState({
  title,
  description,
  action,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="empty-state">
      <p className="empty-state__title">{title}</p>
      {description && <p>{description}</p>}
      {action && <div style={{ marginTop: 16 }}>{action}</div>}
    </div>
  );
}
