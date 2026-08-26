import { createContext } from 'react';

/** Sesion del usuario autenticado, tal como se conserva en el navegador. */
export interface UserSession {
  userId: string;
  userName: string;
  fullName: string;
  roles: string[];
  expiresAtUtc: string;
}

/** Valor expuesto por el contexto de autenticacion. */
export interface AuthenticationContextValue {
  session: UserSession | null;
  isAuthenticated: boolean;
  isRestoringSession: boolean;
  login: (userName: string, password: string) => Promise<void>;
  logout: () => void;
  hasAnyRole: (...roleNames: string[]) => boolean;
  canWriteMaintenance: boolean;
  isAdministrator: boolean;
}

/**
 * Contexto de la sesion.
 *
 * Se declara en un modulo aparte del componente proveedor para que el archivo
 * del proveedor exporte unicamente componentes: asi la recarga en caliente de
 * Vite funciona sin reiniciar el estado de la aplicacion.
 */
export const AuthenticationContext = createContext<AuthenticationContextValue | undefined>(
  undefined,
);
