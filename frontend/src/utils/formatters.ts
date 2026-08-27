/**
 * Funciones de formato para presentacion.
 *
 * Se centralizan para que los montos, porcentajes y fechas se vean igual en toda
 * la aplicacion y para no repetir la configuracion regional en cada pantalla.
 */

const LOCALE = 'es-DO';
const CURRENCY_CODE = 'DOP';

const currencyFormatter = new Intl.NumberFormat(LOCALE, {
  style: 'currency',
  currency: CURRENCY_CODE,
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const numberFormatter = new Intl.NumberFormat(LOCALE, {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const percentageFormatter = new Intl.NumberFormat(LOCALE, {
  style: 'percent',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const dateTimeFormatter = new Intl.DateTimeFormat(LOCALE, {
  dateStyle: 'medium',
  timeStyle: 'short',
});

const dateFormatter = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium' });

/** Formatea un importe como moneda dominicana. */
export function formatCurrency(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '-';
  }

  return currencyFormatter.format(value);
}

/** Formatea un numero con dos decimales. */
export function formatNumber(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '-';
  }

  return numberFormatter.format(value);
}

/** Formatea una fraccion decimal como porcentaje. */
export function formatPercentage(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '-';
  }

  return percentageFormatter.format(value);
}

/** Reconoce una fecha sin hora, en formato AAAA-MM-DD. */
const DATE_ONLY_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/;

/**
 * Interpreta un valor de fecha recibido de la API.
 *
 * Una fecha sin hora (por ejemplo, el inicio de un periodo de nomina) se
 * construye como fecha local. Si se dejara a <c>new Date</c>, la cadena
 * "2026-08-24" se interpretaria como medianoche UTC y al mostrarla en una zona
 * al oeste de Greenwich aparaceria el dia anterior. Los valores con hora si
 * llevan zona y se interpretan tal cual.
 *
 * @param value Valor recibido de la API.
 * @returns Fecha interpretada.
 */
function parseApiDate(value: string): Date {
  const dateOnlyMatch = DATE_ONLY_PATTERN.exec(value);

  if (dateOnlyMatch !== null) {
    return new Date(
      Number(dateOnlyMatch[1]),
      Number(dateOnlyMatch[2]) - 1,
      Number(dateOnlyMatch[3]),
    );
  }

  return new Date(value);
}

/** Formatea una fecha y hora en formato local. */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return '-';
  }

  return dateTimeFormatter.format(parseApiDate(value));
}

/** Formatea una fecha en formato local. */
export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '-';
  }

  return dateFormatter.format(parseApiDate(value));
}
