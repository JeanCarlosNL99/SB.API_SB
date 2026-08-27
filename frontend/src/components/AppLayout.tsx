import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuthentication } from '@/hooks/useAuthentication';
import { BrandLogo } from './BrandLogo';
import {
  EventLogIcon,
  HistoryIcon,
  HomeIcon,
  InstitutionIcon,
  LogoutIcon,
  PayrollIcon,
  PeopleIcon,
  SearchIcon,
  ShieldIcon,
} from './Icons';

/** Descripcion de un elemento del menu lateral. */
interface NavigationItem {
  to: string;
  label: string;
  icon: JSX.Element;
  requiresAdministrator?: boolean;
}

/** Grupo de elementos del menu lateral. */
interface NavigationGroup {
  label?: string;
  items: NavigationItem[];
}

/**
 * Menu de navegacion, declarado como datos.
 *
 * Definir el menu como una estructura y no como marcado repetido permite
 * filtrarlo por permisos y agregar secciones sin duplicar codigo.
 */
const NAVIGATION_GROUPS: NavigationGroup[] = [
  {
    items: [{ to: '/inicio', label: 'Inicio', icon: <HomeIcon /> }],
  },
  {
    label: 'Entidades gubernamentales',
    items: [{ to: '/entidades', label: 'Consulta', icon: <SearchIcon /> }],
  },
  {
    label: 'Nomina',
    items: [
      { to: '/empleados', label: 'Empleados', icon: <PeopleIcon /> },
      { to: '/nomina', label: 'Calcular pago semanal', icon: <PayrollIcon /> },
      { to: '/nomina/historial', label: 'Historial de pagos', icon: <HistoryIcon /> },
    ],
  },
  {
    label: 'Seguridad',
    items: [
      {
        to: '/usuarios',
        label: 'Usuarios y roles',
        icon: <ShieldIcon />,
        requiresAdministrator: true,
      },
      {
        to: '/registro-eventos',
        label: 'Registro de eventos',
        icon: <EventLogIcon />,
        requiresAdministrator: true,
      },
    ],
  },
];

/** Titulo y descripcion que se muestran en el encabezado de cada ruta. */
const PAGE_HEADERS: Record<string, { title: string; subtitle: string }> = {
  '/inicio': {
    title: 'Inicio',
    subtitle: 'Resumen de los mantenimientos y de la nomina semanal',
  },
  '/entidades': {
    title: 'Entidades gubernamentales',
    subtitle: 'Consulta del listado oficial de la Republica Dominicana',
  },
  '/empleados': {
    title: 'Empleados',
    subtitle: 'Gestion de empleados y calculo de pago semanal',
  },
  '/nomina': {
    title: 'Calculo de pago semanal',
    subtitle: 'Genere la nomina de una semana por entidad gubernamental',
  },
  '/nomina/historial': {
    title: 'Historial de pagos semanales',
    subtitle: 'Nominas generadas en semanas anteriores',
  },
  '/registro-eventos': {
    title: 'Registro de eventos',
    subtitle: 'Eventos de la aplicacion registrados por Serilog',
  },
  '/usuarios': {
    title: 'Usuarios y roles',
    subtitle: 'Administracion de accesos al sistema',
  },
};

/**
 * Estructura visual comun a todas las pantallas autenticadas: barra lateral
 * azul institucional, encabezado con el nombre de la pagina y panel gris de
 * contenido, siguiendo la maqueta entregada.
 */
export function AppLayout() {
  const { session, logout, isAdministrator } = useAuthentication();
  const location = useLocation();

  const pageHeader = PAGE_HEADERS[location.pathname] ?? {
    title: 'Nombre de la pagina',
    subtitle: '',
  };

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar__brand">
          <BrandLogo />
        </div>

        <nav className="sidebar__nav" aria-label="Menu principal">
          {NAVIGATION_GROUPS.map((group, groupIndex) => {
            const visibleItems = group.items.filter((item) => {
              if (item.requiresAdministrator) {
                return isAdministrator;
              }

              return true;
            });

            if (visibleItems.length === 0) {
              return null;
            }

            return (
              <div key={group.label ?? `grupo-${groupIndex}`}>
                {group.label && <p className="sidebar__group-label">{group.label}</p>}
                {visibleItems.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    end
                    className={({ isActive }) =>
                      isActive ? 'sidebar__link sidebar__link--active' : 'sidebar__link'
                    }
                  >
                    <span className="sidebar__link-icon">{item.icon}</span>
                    {item.label}
                  </NavLink>
                ))}
              </div>
            );
          })}
        </nav>

        <div className="sidebar__footer">
          <span className="sidebar__user-name">{session?.fullName}</span>
          <span className="sidebar__user-roles">{session?.roles.join(', ')}</span>
          <button type="button" className="sidebar__link" onClick={logout}>
            <span className="sidebar__link-icon">
              <LogoutIcon />
            </span>
            Cerrar sesion
          </button>
        </div>
      </aside>

      <main className="app-main">
        <header className="app-main__header">
          <div>
            <h1 className="app-main__title">{pageHeader.title}</h1>
            {pageHeader.subtitle && (
              <p className="app-main__subtitle">{pageHeader.subtitle}</p>
            )}
          </div>
          <span className="app-main__subtitle">
            <InstitutionIcon size={14} /> Superintendencia de Bancos
          </span>
        </header>

        <div className="app-panel">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
