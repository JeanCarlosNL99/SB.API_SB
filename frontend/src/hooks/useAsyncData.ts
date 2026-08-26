import { useCallback, useEffect, useRef, useState } from 'react';

/** Estado de una carga asincrona de datos. */
interface AsyncDataState<TData> {
  data: TData | null;
  isLoading: boolean;
  error: unknown;
}

/** Resultado devuelto por el hook, con la accion para recargar. */
interface AsyncDataResult<TData> extends AsyncDataState<TData> {
  reload: () => Promise<void>;
  setData: (data: TData | null) => void;
}

/**
 * Carga datos de la API y expone el estado de la operacion.
 *
 * Encapsula el patron cargar/cargando/error que necesitan todas las pantallas y
 * descarta el resultado si el componente ya se desmonto, evitando actualizar el
 * estado de un componente que no existe.
 *
 * @param loadData Funcion que obtiene los datos.
 * @param dependencies Dependencias que, al cambiar, disparan una nueva carga.
 */
export function useAsyncData<TData>(
  loadData: () => Promise<TData>,
  dependencies: unknown[],
): AsyncDataResult<TData> {
  const [state, setState] = useState<AsyncDataState<TData>>({
    data: null,
    isLoading: true,
    error: null,
  });

  const isMountedRef = useRef(true);

  useEffect(() => {
    isMountedRef.current = true;

    return () => {
      isMountedRef.current = false;
    };
  }, []);

  const execute = useCallback(async () => {
    setState((previousState) => ({ ...previousState, isLoading: true, error: null }));

    try {
      const data = await loadData();

      if (isMountedRef.current) {
        setState({ data, isLoading: false, error: null });
      }
    } catch (error) {
      if (isMountedRef.current) {
        setState({ data: null, isLoading: false, error });
      }
    }
    // La funcion de carga se recrea en cada render, por lo que las dependencias
    // reales de la consulta las declara quien usa el hook.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, dependencies);

  useEffect(() => {
    void execute();
  }, [execute]);

  const setData = useCallback((data: TData | null) => {
    setState((previousState) => ({ ...previousState, data }));
  }, []);

  return { ...state, reload: execute, setData };
}
