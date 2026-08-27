import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from '@/components/AppLayout';
import { LoadingIndicator } from '@/components/Feedback';
import { useAuthentication } from '@/hooks/useAuthentication';
import { EmployeesPage } from '@/pages/EmployeesPage';
import { EventLogPage } from '@/pages/EventLogPage';
import { GovernmentEntitiesPage } from '@/pages/GovernmentEntitiesPage';
import { HomePage } from '@/pages/HomePage';
import { LoginPage } from '@/pages/LoginPage';
import { PayrollHistoryPage } from '@/pages/PayrollHistoryPage';
import { PayrollPage } from '@/pages/PayrollPage';
import { UsersPage } from '@/pages/UsersPage';
import type { ReactNode } from 'react';

/**
 * Definicion de rutas de la aplicacion.
 *
 * Las rutas protegidas se envuelven en guardas: la interfaz oculta lo que el
 * usuario no puede usar, pero la autorizacion real la aplica la API. Ocultar una
 * opcion en el cliente es comodidad, no seguridad.
 */
export function App() {
  return (
    <Routes>
      <Route path="/iniciar-sesion" element={<LoginPage />} />

      <Route
        element={
          <RequireAuthentication>
            <AppLayout />
          </RequireAuthentication>
        }
      >
        <Route path="/inicio" element={<HomePage />} />

        <Route path="/entidades" element={<GovernmentEntitiesPage />} />

        <Route path="/empleados" element={<EmployeesPage />} />
        <Route path="/nomina" element={<PayrollPage />} />
        <Route path="/nomina/historial" element={<PayrollHistoryPage />} />

        <Route
          path="/usuarios"
          element={
            <RequireAdministrator>
              <UsersPage />
            </RequireAdministrator>
          }
        />
        <Route
          path="/registro-eventos"
          element={
            <RequireAdministrator>
              <EventLogPage />
            </RequireAdministrator>
          }
        />
      </Route>

      <Route path="/" element={<Navigate to="/inicio" replace />} />
      <Route path="*" element={<Navigate to="/inicio" replace />} />
    </Routes>
  );
}

function RequireAuthentication({ children }: { children: ReactNode }) {
  const { isAuthenticated, isRestoringSession } = useAuthentication();

  if (isRestoringSession) {
    return <LoadingIndicator label="Restaurando sesion..." />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/iniciar-sesion" replace />;
  }

  return <>{children}</>;
}

function RequireAdministrator({ children }: { children: ReactNode }) {
  const { isAdministrator } = useAuthentication();

  if (!isAdministrator) {
    return <Navigate to="/inicio" replace />;
  }

  return <>{children}</>;
}
