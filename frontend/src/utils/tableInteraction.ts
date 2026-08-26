import type { MouseEvent } from 'react';

/**
 * Utilidades para hacer activables las filas de una tabla con un clic.
 *
 * Se centralizan aqui para que todas las tablas de la aplicacion reaccionen
 * igual y para resolver una sola vez los dos casos que rompen esta interaccion:
 * el clic sobre un boton de acciones dentro de la fila y el clic que en realidad
 * es una seleccion de texto.
 */

/** Elementos que ya tienen su propia accion y no deben activar la fila. */
const INTERACTIVE_ELEMENT_SELECTOR = 'button, a, input, select, textarea, label';

/** Clase que aporta la retroalimentacion visual de fila activable. */
export const CLICKABLE_ROW_CLASS_NAME = 'table__row--clickable';

/** Propiedades a aplicar sobre un elemento de fila para volverlo activable. */
export interface ClickableRowProps {
  className: string;
  title: string;
  onClick: (mouseEvent: MouseEvent<HTMLTableRowElement>) => void;
}

/**
 * Construye las propiedades de una fila activable con un clic.
 *
 * La accion tambien esta disponible en el boton de la propia fila, que si es
 * alcanzable con el teclado. Por eso la fila no se agrega al orden de
 * tabulacion: hacerlo duplicaria una parada de teclado por cada registro de la
 * tabla sin ofrecer nada nuevo.
 *
 * @param onActivate Accion a ejecutar cuando se activa la fila.
 * @param description Texto descriptivo que se muestra al posar el cursor.
 * @returns Propiedades listas para expandirse sobre el elemento de la fila.
 */
export function buildClickableRowProps(
  onActivate: () => void,
  description: string,
): ClickableRowProps {
  return {
    className: CLICKABLE_ROW_CLASS_NAME,
    title: description,
    onClick: (mouseEvent) => {
      const clickedElement = mouseEvent.target as HTMLElement;

      if (clickedElement.closest(INTERACTIVE_ELEMENT_SELECTOR) !== null) {
        return;
      }

      const selectedText = window.getSelection()?.toString() ?? '';

      if (selectedText.length > 0) {
        return;
      }

      onActivate();
    },
  };
}
