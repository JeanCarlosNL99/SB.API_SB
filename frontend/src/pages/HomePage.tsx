import { Link } from 'react-router-dom';
import {
  employeesApi,
  governmentEntitiesApi,
  payrollReportsApi,
} from '@/api/endpoints';
import { ErrorMessage, LoadingIndicator } from '@/components/Feedback';
import {
  InstitutionIcon,
  PayrollIcon,
  PeopleIcon,
  SearchIcon,
} from '@/components/Icons';
import { useAsyncData } from '@/hooks/useAsyncData';
import { formatCurrency, formatDateTime } from '@/utils/formatters';

/** Resumen que alimenta la pantalla de inicio. */
interface HomeSummary {
  governmentEntityCount: number;
  employeeCount: number;
  activeEmployeeCount: number;
  totalWeeklyPayment: number;
  generatedAtUtc: string;
}

/**
 * Pantalla de inicio.
 *
 * Consolida los indicadores de los tres modulos en una sola vista. Las consultas
 * se lanzan en paralelo porque son independientes entre si: esperar una detras de
 * otra triplicaria el tiempo de carga sin ninguna ganancia.
 */
export function HomePage() {
  const { data, isLoading, error } = useAsyncData<HomeSummary>(async () => {
    const [entitiesPage, employeesPage, weeklyReport] = await Promise.all([
      governmentEntitiesApi.search({ pageNumber: 1, pageSize: 1 }),
      employeesApi.search({ pageNumber: 1, pageSize: 1 }),
      payrollReportsApi.getWeeklyReport(true),
    ]);

    return {
      governmentEntityCount: entitiesPage.totalCount,
      employeeCount: employeesPage.totalCount,
      activeEmployeeCount: weeklyReport.employeeCount,
      totalWeeklyPayment: weeklyReport.totalWeeklyPayment,
      generatedAtUtc: weeklyReport.generatedAtUtc,
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
          label="Entidades gubernamentales"
          value={data.governmentEntityCount.toLocaleString('es-DO')}
        />
        <MetricCard
          icon={<PeopleIcon size={22} />}
          label="Empleados registrados"
          value={data.employeeCount.toLocaleString('es-DO')}
        />
        <MetricCard
          icon={<PeopleIcon size={22} />}
          label="Empleados activos"
          value={data.activeEmployeeCount.toLocaleString('es-DO')}
        />
        <MetricCard
          icon={<PayrollIcon size={22} />}
          label="Nomina semanal"
          value={formatCurrency(data.totalWeeklyPayment)}
        />
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
            to="/entidades"
            icon={<SearchIcon size={22} />}
            title="Consultar entidades"
            description="Listado oficial con filtros por categoria, sector y poder del Estado."
          />
          <QuickLink
            to="/empleados"
            icon={<PeopleIcon size={22} />}
            title="Gestionar empleados"
            description="Alta, edicion y filtros por nombre, departamento y estado."
          />
          <QuickLink
            to="/nomina"
            icon={<PayrollIcon size={22} />}
            title="Reporte de nomina"
            description="Pago semanal con el detalle del calculo por tipo de contrato."
          />
        </div>
      </section>

      <section className="card">
        <h2 className="card__title">Ultima generacion del reporte</h2>
        <div className="detail-row">
          <span className="detail-row__label">Fecha y hora</span>
          <span className="detail-row__value">{formatDateTime(data.generatedAtUtc)}</span>
        </div>
        <div className="detail-row">
          <span className="detail-row__label">Alcance</span>
          <span className="detail-row__value">Solo empleados activos</span>
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
