import { useCallback, useState } from 'react';
import { eventLogApi } from '@/api/payrollEndpoints';
import { EmptyState, ErrorMessage, LoadingIndicator } from '@/components/Feedback';
import { useAsyncData } from '@/hooks/useAsyncData';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { formatDateTime } from '@/utils/formatters';
import type {
  EventLogFile,
  EventLogFilter,
  EventLogLevel,
  EventLogResult,
} from '@/types/api';

/** Niveles ofrecidos en el filtro, del mas detallado al mas grave. */
const LEVEL_OPTIONS: { value: EventLogLevel; label: string }[] = [
  { value: 'Verbose', label: 'Verbose y superiores' },
  { value: 'Debug', label: 'Debug y superiores' },
  { value: 'Information', label: 'Information y superiores' },
  { value: 'Warning', label: 'Warning y superiores' },
  { value: 'Error', label: 'Error y superiores' },
  { value: 'Fatal', label: 'Solo Fatal' },
];

const ENTRY_COUNT_OPTIONS = [50, 100, 200, 500, 1000];

const INITIAL_FILTER: EventLogFilter = {
  fileName: '',
  minimumLevel: '',
  searchTerm: '',
  maximumEntries: 200,
};

/** Clase de la pildora segun la gravedad del evento. */
const LEVEL_BADGE_CLASS: Record<string, string> = {
  Verbose: 'badge badge--inactive',
  Debug: 'badge badge--inactive',
  Information: 'badge badge--type',
  Warning: 'badge badge--role',
  Error: 'badge badge--danger-level',
  Fatal: 'badge badge--danger-level',
};

/**
 * Registro de eventos de la aplicacion.
 *
 * Solo visible para el rol administrador, tanto en el menu como en la API: el
 * registro contiene rutas internas, trazas de excepciones y nombres de usuario.
 * Lee los archivos que escribe Serilog en formato JSON, lo que permite filtrar
 * por nivel y buscar en el mensaje o en la excepcion.
 */
export function EventLogPage() {
  const [filter, setFilter] = useState<EventLogFilter>(INITIAL_FILTER);
  const [expandedEntryIndex, setExpandedEntryIndex] = useState<number | null>(null);
  const [reloadToken, setReloadToken] = useState(0);

  const debouncedSearchTerm = useDebouncedValue(filter.searchTerm ?? '');

  const filesQuery = useAsyncData<EventLogFile[]>(() => eventLogApi.getFiles(), []);

  const logQuery = useAsyncData<EventLogResult>(
    () => eventLogApi.read({ ...filter, searchTerm: debouncedSearchTerm }),
    [
      filter.fileName,
      filter.minimumLevel,
      filter.maximumEntries,
      debouncedSearchTerm,
      reloadToken,
    ],
  );

  const updateFilter = useCallback((changes: Partial<EventLogFilter>) => {
    setFilter((previousFilter) => ({ ...previousFilter, ...changes }));
    setExpandedEntryIndex(null);
  }, []);

  const isRefreshing = logQuery.isLoading && logQuery.data !== null;

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Filtros del registro</h2>
            <p className="card__description">
              Entradas de la mas reciente a la mas antigua. Escriba para buscar en el mensaje,
              en la excepcion o en el identificador de correlacion.
            </p>
          </div>
          <button
            type="button"
            className="button button--primary"
            onClick={() => {
              setReloadToken((previousToken) => previousToken + 1);
              void filesQuery.reload();
            }}
            disabled={logQuery.isLoading}
          >
            {logQuery.isLoading ? 'Actualizando...' : 'Actualizar'}
          </button>
        </div>

        <div className="filters">
          <div className="field">
            <label className="field__label" htmlFor="logFile">
              Archivo
            </label>
            <select
              id="logFile"
              className="control"
              value={filter.fileName ?? ''}
              onChange={(changeEvent) => updateFilter({ fileName: changeEvent.target.value })}
            >
              <option value="">Mas reciente</option>
              {(filesQuery.data ?? []).map((file) => (
                <option key={file.fileName} value={file.fileName}>
                  {file.fileName} ({formatFileSize(file.sizeInBytes)})
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label className="field__label" htmlFor="logLevel">
              Nivel minimo
            </label>
            <select
              id="logLevel"
              className="control"
              value={filter.minimumLevel ?? ''}
              onChange={(changeEvent) =>
                updateFilter({
                  minimumLevel: changeEvent.target.value as EventLogLevel | '',
                })
              }
            >
              <option value="">Todos los niveles</option>
              {LEVEL_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label className="field__label" htmlFor="logSearch">
              Buscar
            </label>
            <input
              id="logSearch"
              className="control"
              type="search"
              placeholder="Mensaje, excepcion o correlacion"
              autoComplete="off"
              value={filter.searchTerm ?? ''}
              onChange={(changeEvent) => updateFilter({ searchTerm: changeEvent.target.value })}
            />
          </div>

          <div className="field">
            <label className="field__label" htmlFor="logMaximumEntries">
              Entradas
            </label>
            <select
              id="logMaximumEntries"
              className="control"
              value={filter.maximumEntries}
              onChange={(changeEvent) =>
                updateFilter({ maximumEntries: Number(changeEvent.target.value) })
              }
            >
              {ENTRY_COUNT_OPTIONS.map((option) => (
                <option key={option} value={option}>
                  Ultimas {option}
                </option>
              ))}
            </select>
          </div>
        </div>

        {logQuery.data && (
          <div className="filters__status" aria-live="polite">
            {isRefreshing ? (
              <>
                <span className="spinner" />
                Leyendo el registro...
              </>
            ) : (
              <>
                {logQuery.data.entryCount} entrada(s) de {logQuery.data.fileName}
                {Object.entries(logQuery.data.countsByLevel)
                  .sort(([firstLevel], [secondLevel]) =>
                    firstLevel.localeCompare(secondLevel),
                  )
                  .map(([level, count]) => (
                    <span
                      key={level}
                      className={LEVEL_BADGE_CLASS[level] ?? 'badge badge--type'}
                      style={{ marginLeft: 6 }}
                    >
                      {level}: {count}
                    </span>
                  ))}
              </>
            )}
          </div>
        )}
      </section>

      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Eventos</h2>
            <p className="card__description">
              Seleccione una entrada con excepcion para ver la traza completa.
            </p>
          </div>
        </div>

        <ErrorMessage error={logQuery.error} />

        {logQuery.isLoading && logQuery.data === null && (
          <LoadingIndicator label="Leyendo el registro de eventos..." />
        )}

        {logQuery.data && (
          <div className={isRefreshing ? 'is-refreshing' : undefined}>
            {logQuery.data.entries.length === 0 ? (
              <EmptyState
                title="No hay entradas que coincidan"
                description="Ajuste el nivel minimo o el texto buscado."
              />
            ) : (
              <div className="table-wrapper">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Fecha y hora</th>
                      <th>Nivel</th>
                      <th>Mensaje</th>
                      <th>Usuario</th>
                      <th>Correlacion</th>
                    </tr>
                  </thead>
                  <tbody>
                    {logQuery.data.entries.map((entry, entryIndex) => {
                      const hasException =
                        entry.exception !== null && entry.exception !== undefined;
                      const isExpanded = expandedEntryIndex === entryIndex;

                      return (
                        <EventLogRow
                          key={`${entry.timestamp}-${entryIndex}`}
                          timestamp={entry.timestamp}
                          level={entry.level}
                          message={entry.message}
                          userName={entry.userName}
                          correlationId={entry.correlationId}
                          sourceContext={entry.sourceContext}
                          exception={entry.exception}
                          hasException={hasException}
                          isExpanded={isExpanded}
                          onToggle={() =>
                            setExpandedEntryIndex(isExpanded ? null : entryIndex)
                          }
                        />
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}

            {logQuery.data.hasMoreEntries && (
              <p className="field__hint" style={{ marginTop: 12 }}>
                El archivo contiene mas entradas de las mostradas. Aumente la cantidad de
                entradas o afine el filtro.
              </p>
            )}
          </div>
        )}
      </section>
    </>
  );
}

function EventLogRow({
  timestamp,
  level,
  message,
  userName,
  correlationId,
  sourceContext,
  exception,
  hasException,
  isExpanded,
  onToggle,
}: {
  timestamp: string;
  level: string;
  message: string;
  userName?: string | null;
  correlationId?: string | null;
  sourceContext?: string | null;
  exception?: string | null;
  hasException: boolean;
  isExpanded: boolean;
  onToggle: () => void;
}) {
  return (
    <>
      <tr
        className={hasException ? 'table__row--clickable' : undefined}
        title={hasException ? 'Ver la traza de la excepcion' : undefined}
        onClick={hasException ? onToggle : undefined}
      >
        <td className="table td--numeric">{formatDateTime(timestamp)}</td>
        <td>
          <span className={LEVEL_BADGE_CLASS[level] ?? 'badge badge--type'}>{level}</span>
        </td>
        <td className="table td--wrap">
          {message}
          {hasException && (
            <>
              <br />
              <span className="field__hint">
                {isExpanded ? 'Ocultar traza' : 'Contiene excepcion — clic para ver la traza'}
              </span>
            </>
          )}
        </td>
        <td>{userName ?? '-'}</td>
        <td className="table td--numeric">{correlationId ?? '-'}</td>
      </tr>
      {isExpanded && hasException && (
        <tr>
          <td colSpan={5} className="table td--wrap">
            {sourceContext && (
              <p className="field__hint" style={{ marginBottom: 8 }}>
                Origen: {sourceContext}
              </p>
            )}
            <pre className="stack-trace">{exception}</pre>
          </td>
        </tr>
      )}
    </>
  );
}

/** Formatea un tamano en bytes de forma legible. */
function formatFileSize(sizeInBytes: number): string {
  const kilobyte = 1024;
  const megabyte = kilobyte * 1024;

  if (sizeInBytes >= megabyte) {
    return `${(sizeInBytes / megabyte).toFixed(1)} MB`;
  }

  return `${Math.max(1, Math.round(sizeInBytes / kilobyte))} KB`;
}
