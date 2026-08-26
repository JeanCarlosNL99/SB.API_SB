import { useEffect, useState } from 'react';

/**
 * Retardo por omision, en milisegundos, antes de aplicar un filtro de texto.
 *
 * Es el punto medio entre dos molestias: un valor mas bajo lanza peticiones que
 * el usuario nunca llega a leer, y un valor mas alto hace que la tabla se sienta
 * lenta al escribir.
 */
export const SEARCH_DEBOUNCE_IN_MILLISECONDS = 300;

/**
 * Devuelve el valor recibido, pero solo despues de que haya dejado de cambiar
 * durante el retardo indicado.
 *
 * Permite que el campo de busqueda responda de inmediato en pantalla mientras la
 * consulta al servidor se lanza una sola vez, cuando el usuario deja de escribir.
 *
 * @param value Valor que cambia con cada pulsacion.
 * @param delayInMilliseconds Tiempo de inactividad requerido antes de propagarlo.
 * @returns El ultimo valor estable.
 */
export function useDebouncedValue<TValue>(
  value: TValue,
  delayInMilliseconds: number = SEARCH_DEBOUNCE_IN_MILLISECONDS,
): TValue {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const timeoutIdentifier = window.setTimeout(() => {
      setDebouncedValue(value);
    }, delayInMilliseconds);

    // Cada cambio cancela el temporizador anterior: solo sobrevive el ultimo.
    return () => window.clearTimeout(timeoutIdentifier);
  }, [value, delayInMilliseconds]);

  return debouncedValue;
}
