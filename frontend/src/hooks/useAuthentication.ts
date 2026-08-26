import { useContext } from 'react';
import {
  AuthenticationContext,
  type AuthenticationContextValue,
} from '@/context/authenticationContext';

/**
 * Obtiene la sesion actual.
 *
 * Falla de inmediato si se usa fuera del proveedor, lo que convierte un error de
 * composicion de componentes en un error evidente durante el desarrollo.
 */
export function useAuthentication(): AuthenticationContextValue {
  const contextValue = useContext(AuthenticationContext);

  if (contextValue === undefined) {
    throw new Error('useAuthentication debe usarse dentro de un AuthenticationProvider.');
  }

  return contextValue;
}
