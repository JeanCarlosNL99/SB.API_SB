import type { SVGProps } from 'react';

/**
 * Conjunto de iconos SVG en linea.
 *
 * Se dibujan como componentes en lugar de usar una libreria de iconos: son pocos,
 * heredan el color del texto mediante currentColor y no agregan peso ni
 * dependencias al paquete final.
 */

const DEFAULT_ICON_SIZE = 18;

type IconProps = SVGProps<SVGSVGElement> & { size?: number };

function IconBase({ size = DEFAULT_ICON_SIZE, children, ...svgProps }: IconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
      {...svgProps}
    >
      {children}
    </svg>
  );
}

/** Icono de inicio, equivalente al icono adjunto en el requerimiento. */
export function HomeIcon(props: IconProps) {
  return (
    <IconBase {...props} fill="currentColor" stroke="none">
      <path d="M11.3 2.4a1.1 1.1 0 0 1 1.4 0l8.2 7a1.1 1.1 0 0 1-.7 1.9h-1.4v8.6a1.4 1.4 0 0 1-1.4 1.4h-3.6v-5.6a1 1 0 0 0-1-1h-2a1 1 0 0 0-1 1v5.6H6.2a1.4 1.4 0 0 1-1.4-1.4v-8.6H3.4a1.1 1.1 0 0 1-.7-1.9l8.6-7z" />
    </IconBase>
  );
}

/** Icono de consulta o busqueda. */
export function SearchIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-3.6-3.6" />
    </IconBase>
  );
}

/** Icono de creacion de registro. */
export function PlusIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M12 5v14M5 12h14" />
    </IconBase>
  );
}

/** Icono de empleados. */
export function PeopleIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M16 20v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M22 20v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8" />
    </IconBase>
  );
}

/** Icono de nomina o reporte de pagos. */
export function PayrollIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <rect x="2" y="5" width="20" height="14" rx="2" />
      <circle cx="12" cy="12" r="3" />
      <path d="M6 12h.01M18 12h.01" />
    </IconBase>
  );
}

/** Icono de administracion de usuarios. */
export function ShieldIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M12 2.5 4.5 5.5v6c0 4.6 3.1 8.7 7.5 10 4.4-1.3 7.5-5.4 7.5-10v-6L12 2.5z" />
      <path d="m9.2 12.2 2 2 3.6-3.6" />
    </IconBase>
  );
}

/** Icono de edicion. */
export function EditIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
    </IconBase>
  );
}

/** Icono de eliminacion. */
export function TrashIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2" />
      <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
      <path d="M10 11v6M14 11v6" />
    </IconBase>
  );
}

/** Icono de cierre. */
export function CloseIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M18 6 6 18M6 6l12 12" />
    </IconBase>
  );
}

/** Icono de cierre de sesion. */
export function LogoutIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <path d="m16 17 5-5-5-5M21 12H9" />
    </IconBase>
  );
}

/** Icono de detalle o vista. */
export function DetailIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7z" />
      <circle cx="12" cy="12" r="3" />
    </IconBase>
  );
}

/** Icono de entidades gubernamentales. */
export function InstitutionIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M3 10 12 4l9 6" />
      <path d="M5 10v10h14V10" />
      <path d="M9 20v-6h6v6" />
    </IconBase>
  );
}

/** Icono de compania. */
export function CompanyIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <rect x="3" y="7" width="12" height="14" rx="1.5" />
      <path d="M15 11h5a1 1 0 0 1 1 1v9h-6" />
      <path d="M7 11h2M7 15h2M7 19h2M12 11h.01M12 15h.01M12 19h.01" />
      <path d="M3 7l6-4 6 4" />
    </IconBase>
  );
}

/** Icono de historial. */
export function HistoryIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M3.5 12a8.5 8.5 0 1 0 2.6-6.1" />
      <path d="M3 4v4h4" />
      <path d="M12 8v4.5l3 1.8" />
    </IconBase>
  );
}

/** Icono del registro de eventos. */
export function EventLogIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M5 3h9l5 5v13a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z" />
      <path d="M14 3v5h5" />
      <path d="M8 13h8M8 17h5" />
    </IconBase>
  );
}
