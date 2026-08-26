import { useEffect, type ReactNode } from 'react';
import { CloseIcon } from './Icons';

/**
 * Ventana modal accesible.
 *
 * Cierra con la tecla Escape y con un clic fuera del contenido, y bloquea el
 * desplazamiento del fondo mientras esta abierta. Al declararla una sola vez,
 * los formularios de alta y edicion de todos los mantenimientos comparten el
 * mismo comportamiento.
 */
export function Modal({
  title,
  description,
  isOpen,
  onClose,
  children,
}: {
  title: string;
  description?: string;
  isOpen: boolean;
  onClose: () => void;
  children: ReactNode;
}) {
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    function handleKeyDown(keyboardEvent: KeyboardEvent) {
      if (keyboardEvent.key === 'Escape') {
        onClose();
      }
    }

    const previousOverflow = document.body.style.overflow;

    document.body.style.overflow = 'hidden';
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose]);

  if (!isOpen) {
    return null;
  }

  return (
    <div
      className="modal-backdrop"
      role="presentation"
      onClick={(mouseEvent) => {
        if (mouseEvent.target === mouseEvent.currentTarget) {
          onClose();
        }
      }}
    >
      <div className="modal" role="dialog" aria-modal="true" aria-label={title}>
        <div className="modal__header">
          <div>
            <h2 className="modal__title">{title}</h2>
            {description && <p className="modal__description">{description}</p>}
          </div>
          <button
            type="button"
            className="button button--icon"
            onClick={onClose}
            aria-label="Cerrar"
          >
            <CloseIcon />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

/**
 * Dialogo de confirmacion para operaciones destructivas. Se exige una
 * confirmacion explicita antes de eliminar cualquier registro.
 */
export function ConfirmationDialog({
  isOpen,
  title,
  message,
  confirmLabel = 'Eliminar',
  isProcessing = false,
  onConfirm,
  onCancel,
}: {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  isProcessing?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <Modal title={title} isOpen={isOpen} onClose={onCancel}>
      <p>{message}</p>
      <div className="form-actions">
        <button
          type="button"
          className="button button--secondary"
          onClick={onCancel}
          disabled={isProcessing}
        >
          Cancelar
        </button>
        <button
          type="button"
          className="button button--danger"
          onClick={onConfirm}
          disabled={isProcessing}
        >
          {isProcessing ? 'Procesando...' : confirmLabel}
        </button>
      </div>
    </Modal>
  );
}
