/**
 * Utilidades de la semana de nomina en el cliente.
 *
 * El calculo de la semana ISO 8601 se replica aqui porque el control
 * <input type="week"> del navegador trabaja con el formato "AAAA-Wnn" y hay que
 * traducirlo al par (ano, semana) que espera la API. No se duplica ninguna regla
 * de negocio: la validez de la semana y las reglas de pago las decide el
 * servidor.
 */

const DAYS_IN_WEEK = 7;
const THURSDAY_INDEX = 4;

/** Semana de nomina identificada por ano y numero. */
export interface PayrollWeekValue {
  year: number;
  weekNumber: number;
}

/**
 * Obtiene el ano y el numero de semana ISO 8601 de una fecha.
 *
 * @param date Fecha a evaluar.
 * @returns Ano y numero de semana.
 */
export function getIsoWeek(date: Date): PayrollWeekValue {
  // Se trabaja sobre una copia en UTC para que la zona horaria del navegador no
  // desplace la semana un dia.
  const target = new Date(
    Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()),
  );

  // Se mueve al jueves de la misma semana: por definicion de la norma, el ano de
  // la semana es el ano de su jueves.
  const dayNumber = target.getUTCDay() === 0 ? DAYS_IN_WEEK : target.getUTCDay();

  target.setUTCDate(target.getUTCDate() + THURSDAY_INDEX - dayNumber);

  const year = target.getUTCFullYear();
  const firstThursday = new Date(Date.UTC(year, 0, 4));
  const firstThursdayDayNumber =
    firstThursday.getUTCDay() === 0 ? DAYS_IN_WEEK : firstThursday.getUTCDay();

  firstThursday.setUTCDate(firstThursday.getUTCDate() + THURSDAY_INDEX - firstThursdayDayNumber);

  const millisecondsPerDay = 86_400_000;
  const weekNumber =
    1 +
    Math.round(
      (target.getTime() - firstThursday.getTime()) / (millisecondsPerDay * DAYS_IN_WEEK),
    );

  return { year, weekNumber };
}

/** Devuelve la semana de nomina en curso. */
export function getCurrentWeek(): PayrollWeekValue {
  return getIsoWeek(new Date());
}

/** Devuelve la semana anterior a la indicada. */
export function getPreviousWeek(week: PayrollWeekValue): PayrollWeekValue {
  const monday = getWeekStartDate(week);

  monday.setUTCDate(monday.getUTCDate() - DAYS_IN_WEEK);

  return getIsoWeek(
    new Date(monday.getUTCFullYear(), monday.getUTCMonth(), monday.getUTCDate()),
  );
}

/** Obtiene el lunes de la semana indicada. */
export function getWeekStartDate(week: PayrollWeekValue): Date {
  const firstThursday = new Date(Date.UTC(week.year, 0, 4));
  const firstThursdayDayNumber =
    firstThursday.getUTCDay() === 0 ? DAYS_IN_WEEK : firstThursday.getUTCDay();

  const firstMonday = new Date(firstThursday);

  firstMonday.setUTCDate(firstThursday.getUTCDate() - (firstThursdayDayNumber - 1));
  firstMonday.setUTCDate(firstMonday.getUTCDate() + (week.weekNumber - 1) * DAYS_IN_WEEK);

  return firstMonday;
}

/** Convierte la semana al formato "AAAA-Wnn" que usa el control del navegador. */
export function toInputValue(week: PayrollWeekValue): string {
  return `${week.year}-W${String(week.weekNumber).padStart(2, '0')}`;
}

/**
 * Interpreta el valor de un control <input type="week">.
 *
 * @param inputValue Valor en formato "AAAA-Wnn".
 * @returns La semana, o nulo si el valor no tiene el formato esperado.
 */
export function fromInputValue(inputValue: string): PayrollWeekValue | null {
  const match = /^(\d{4})-W(\d{1,2})$/.exec(inputValue);

  if (match === null) {
    return null;
  }

  return { year: Number(match[1]), weekNumber: Number(match[2]) };
}

/** Etiqueta legible de la semana, coherente con la que devuelve la API. */
export function toLabel(week: PayrollWeekValue): string {
  return `${week.year}-S${String(week.weekNumber).padStart(2, '0')}`;
}
