import { Link } from 'react-router-dom';
import { employeesApi, governmentEntitiesApi } from '@/api/endpoints';
import { payrollApi } from '@/api/payrollEndpoints';
import { ErrorMessage, LoadingIndicator } from '@/components/Feedback';
import {
  HistoryIcon,
  InstitutionIcon,
  PayrollIcon,
  PeopleIcon,
  SearchIcon,
} from '@/components/Icons';
import { useAsyncData } from '@/hooks/useAsyncData';
import { formatCurrency, formatDate, formatDateTime } from '@/utils/formatters';
import { getCurrentWeek, getPreviousWeek, toLabel } from '@/utils/payrollWeek';
import type { PayrollRunSummary } from '@/types/api';

/** Resumen que alimenta la pantalla de inicio. */
interface HomeSummary {
  governmentEntityCount: number;
  employeeCount: number;
  payableEntityCount: number;
  pendingEntityCount: number;
  lastPayrollRuns: PayrollRunSummary[];
}

/**
 * Pantalla de inicio.
 *
 * Consolida los indicadores de los modulos en una sola vista y responde la
 * pregunta operativa del negocio: cuantas entidades tienen pendiente el pago de
 * la semana pasada. Las consultas se lanzan en paralelo porque son independientes
 * entre si.
 */
export function HomePage() {
  const previousWeek = getPreviousWeek(getCurrentWeek());

  const { data, isLoading, error } = useAsyncData<HomeSummary>(async () => {
    const [entitiesPage, employeesPage, payableEntities, historyPage] = await Promise.all([
      governmentEntitiesApi.search({ pageNumber: 1, pageSize: 1 }),
      employeesApi.search({ pageNumber: 1, pageSize: 1 }),
      payrollApi.getPayableEntities(),
      payrollApi.searchHistory({
        year: previousWeek.year,
        includeCancelled: false,
        pageNumber: 1,
        pageSize: 100,
      }),
    ]);

    const entitiesWithActiveEmployees = payableEntities.filter(
      (entity) => entity.activeEmployeeCount > 0,
    );

    const entitiesPaidForPreviousWeek = new Set(
      historyPage.items
        .filter((summary) => summary.weekNumber === previousWeek.weekNumber)
        .map((summary) => summary.governmentEntityId),
    );

    return {
      governmentEntityCount: entitiesPage.totalCount,
      employeeCount: employeesPage.totalCount,
      payableEntityCount: entitiesWithActiveEmployees.length,
      pendingEntityCount: entitiesWithActiveEmployees.filter(
        (entity) => !entitiesPaidForPreviousWeek.has(entity.id),
      ).length,
      lastPayrollRuns: historyPage.items.slice(0, 5),
    };
  }, []);

  if (isLoading) {
    return <LoadingIndicator label="Cargando indicadores..." />;
  }

  if (error) {
    return <ErrorMessage error={error} />;
  }

  if (!data) {
    return null;
  }

  return (
    <>
      <section className="metric-grid">
        <MetricCard
          icon={<InstitutionIcon size={22} />}
          label="Entidades con nomina"
          value={data.payableEntityCount.toLocaleString('es-DO')}
        />
        <MetricCard
          icon={<PeopleIcon size={22} />}
          label="Empleados registrados"
          value={data.employeeCount.toLocaleString('es-DO')}
        />
        <MetricCard
          icon={<PayrollIcon size={22} />}
          label={`Pendientes de pagar ${toLabel(previousWeek)}`}
          value={data.pendingEntityCount.toLocaleString('es-DO')}
        />
        <MetricCard
          icon={<InstitutionIcon size={22} />}
          label="Entidades gubernamentales"
          value={data.governmentEntityCount.toLocaleString('es-DO')}
        />
      </section>

      {data.pendingEntityCount > 0 && (
        <section className="card">
          <div className="alert alert--info" role="status">
            <div>
              <strong>
                {data.pendingEntityCount} entidad(es) sin la nomina de la semana{' '}
                {toLabel(previousWeek)}.
              </strong>
              <p style={{ marginTop: 4 }}>
                La semana termino y sus pagos todavia no se han generado.{' '}
                <Link to="/nomina">Ir al calculo de pago semanal</Link>.
              </p>
            </div>
          </div>
        </section>
      )}

      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Ultimas nominas generadas</h2>
            <p className="card__description">
              Los cinco pagos semanales mas recientes del ano {previousWeek.year}.
            </p>
          </div>
          <Link to="/nomina/historial" className="button button--secondary">
            Ver historial completo
          </Link>
        </div>

        {data.lastPayrollRuns.length === 0 ? (
          <p className="card__description">
            Todavia no hay nominas generadas. Genere la primera desde el calculo de pago
            semanal.
          </p>
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Semana</th>
                  <th>Entidad gubernamental</th>
                  <th className="table th--numeric">Empleados</th>
                  <th className="table th--numeric">Total pagado</th>
                  <th>Generada</th>
                </tr>
              </thead>
              <tbody>
                {data.lastPayrollRuns.map((summary) => (
                  <tr key={summary.id}>
                    <td>
                      <strong>{summary.weekLabel}</strong>
                      <br />
                      <span className="field__hint">
                        {formatDate(summary.weekStartDate)} —{' '}
                        {formatDate(summary.weekEndDate)}
                      </span>
                    </td>
                    <td className="table td--wrap">{summary.governmentEntityName}</td>
                    <td className="table td--numeric">{summary.employeeCount}</td>
                    <td className="table td--numeric">
                      {formatCurrency(summary.totalAmount)}
                    </td>
                    <td>{formatDateTime(summary.generatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Accesos rapidos</h2>
            <p className="card__description">
              Modulos disponibles segun los permisos de su usuario.
            </p>
          </div>
        </div>

        <div className="metric-grid">
          <QuickLink
            to="/nomina"
            icon={<PayrollIcon size={22} />}
            title="Calcular pago semanal"
            description="Revise el calculo de una semana y genere el pago de la entidad."
          />
          <QuickLink
            to="/nomina/historial"
            icon={<HistoryIcon size={22} />}
            title="Historial de pagos"
            description="Nominas de semanas anteriores con el detalle de lo que se pago."
          />
          <QuickLink
            to="/empleados"
            icon={<PeopleIcon size={22} />}
            title="Gestionar empleados"
            description="Alta, edicion y filtros por nombre, entidad, departamento y estado."
          />
          <QuickLink
            to="/entidades"
            icon={<SearchIcon size={22} />}
            title="Consultar entidades"
            description="Listado oficial con filtros por categoria, sector y poder del Estado."
          />
        </div>
      </section>
    </>
  );
}

function MetricCard({
  icon,
  label,
  value,
}: {
  icon: JSX.Element;
  label: string;
  value: string;
}) {
  return (
    <article className="metric-card">
      <span className="metric-card__icon">{icon}</span>
      <div>
        <p className="metric-card__label">{label}</p>
        <p className="metric-card__value">{value}</p>
      </div>
    </article>
  );
}

function QuickLink({
  to,
  icon,
  title,
  description,
}: {
  to: string;
  icon: JSX.Element;
  title: string;
  description: string;
}) {
  return (
    <Link to={to} className="metric-card">
      <span className="metric-card__icon">{icon}</span>
      <div>
        <p style={{ fontWeight: 600 }}>{title}</p>
        <p className="card__description">{description}</p>
      </div>
    </Link>
  );
}
