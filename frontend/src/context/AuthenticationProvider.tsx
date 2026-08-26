import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authenticationApi } from '@/api/endpoints';
import {
  ACCESS_TOKEN_STORAGE_KEY,
  SESSION_STORAGE_KEY,
  UNAUTHORIZED_EVENT_NAME,
} from '@/api/httpClient';
import { ROLE_NAMES } from '@/constants/roles';
import {
  AuthenticationContext,
  type AuthenticationContextValue,
  type UserSession,
} from './authenticationContext';
import type { AuthenticationResponse } from '@/types/api';

/**
 * Proveedor de la sesion del usuario.
 *
 * Mantiene la sesion en un unico lugar y la restaura desde el navegador al
 * recargar la pagina, para que un refresco no obligue a iniciar sesion otra vez
 * mientras el token siga vigente.
 */
export function AuthenticationProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<UserSession | null>(null);
  const [isRestoringSession, setIsRestoringSession] = useState(true);

  const logout = useCallback(() => {
    window.localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY);
    window.localStorage.removeItem(SESSION_STORAGE_KEY);
    setSession(null);
  }, []);

  useEffect(() => {
    setSession(restoreValidSession());
    setIsRestoringSession(false);
  }, []);

  useEffect(() => {
    // La API avisa con un evento cuando rechaza el token: en ese momento se
    // cierra la sesion para que el usuario vuelva a autenticarse.
    window.addEventListener(UNAUTHORIZED_EVENT_NAME, logout);

    return () => window.removeEventListener(UNAUTHORIZED_EVENT_NAME, logout);
  }, [logout]);

  const login = useCallback(async (userName: string, password: string) => {
    const response: AuthenticationResponse = await authenticationApi.login({
      userName,
      password,
    });

    const userSession: UserSession = {
      userId: response.userId,
      userName: response.userName,
      fullName: response.fullName,
      roles: response.roles,
      expiresAtUtc: response.expiresAtUtc,
    };

    window.localStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, response.accessToken);
    window.localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(userSession));

    setSession(userSession);
  }, []);

  const hasAnyRole = useCallback(
    (...roleNames: string[]) =>
      session !== null && roleNames.some((roleName) => session.roles.includes(roleName)),
    [session],
  );

  const contextValue = useMemo<AuthenticationContextValue>(
    () => ({
      session,
      isAuthenticated: session !== null,
      isRestoringSession,
      login,
      logout,
      hasAnyRole,
      canWriteMaintenance: hasAnyRole(
        ROLE_NAMES.ADMINISTRATOR,
        ROLE_NAMES.HUMAN_RESOURCES,
      ),
      isAdministrator: hasAnyRole(ROLE_NAMES.ADMINISTRATOR),
    }),
    [session, isRestoringSession, login, logout, hasAnyRole],
  );

  return (
    <AuthenticationContext.Provider value={contextValue}>
      {children}
    </AuthenticationContext.Provider>
  );
}

/**
 * Recupera la sesion guardada en el navegador y la descarta si el token ya
 * vencio, evitando mostrar una interfaz que la API va a rechazar.
 */
function restoreValidSession(): UserSession | null {
  const accessToken = window.localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY);
  const storedSession = window.localStorage.getItem(SESSION_STORAGE_KEY);

  if (!accessToken || !storedSession) {
    return null;
  }

  try {
    const parsedSession = JSON.parse(storedSession) as UserSession;

    if (new Date(parsedSession.expiresAtUtc).getTime() <= Date.now()) {
      window.localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY);
      window.localStorage.removeItem(SESSION_STORAGE_KEY);

      return null;
    }

    return parsedSession;
  } catch {
    return null;
  }
}
