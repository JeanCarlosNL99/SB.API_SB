import { useState } from 'react';
import { payrollReportsApi } from '@/api/endpoints';
import { EmptyState, ErrorMessage, LoadingIndicator } from '@/components/Feedback';
import { useAsyncData } from '@/hooks/useAsyncData';
import { formatCurrency, formatDateTime } from '@/utils/formatters';
import { buildClickableRowProps } from '@/utils/tableInteraction';
import type { PayrollReportLine, WeeklyPayrollReport } from '@/types/api';

/**
 * Reporte semanal de nomina.
 *
 * Muestra el detalle del calculo tal como lo devuelve la API: cada linea trae su
 * formula y sus componentes, de modo que el usuario puede auditar como se obtuvo
 * cada monto sin salir de la pantalla.
 */
export function PayrollReportPage() {
  const [onlyActiveEmployees, setOnlyActiveEmployees] = useState(true);
  const [expandedEmployeeId, setExpandedEmployeeId] = useState<string | null>(null);

  const reportQuery = useAsyncData<WeeklyPayrollReport>(
    () => payrollReportsApi.getWeeklyReport(onlyActiveEmployees),
    [onlyActiveEmployees],
  );

  if (reportQuery.isLoading) {
    return <LoadingIndicator label="Generando reporte de nomina..." />;
  }

  if (reportQuery.error) {
    return <ErrorMessage error={reportQuery.error} />;
  }

  const report = reportQuery.data;

  if (!report) {
    return null;
  }

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Resumen de la semana</h2>
            <p className="card__description">
              Generado el {formatDateTime(report.generatedAtUtc)}.
            </p>
          </div>
          <label className="field__label" style={{ display: 'flex', gap: 8 }}>
            <input
              type="checkbox"
              checked={onlyActiveEmployees}
              onChange={(changeEvent) => setOnlyActiveEmployees(changeEvent.target.checked)}
            />
            Incluir solo empleados activos
          </label>
        </div>

        <div className="metric-grid">
          <div className="metric-card">
            <div>
              <p className="metric-card__label">Empleados incluidos</p>
              <p className="metric-card__value">{report.employeeCount}</p>
            </div>
          </div>
          <div className="metric-card">
            <div>
              <p className="metric-card__label">Total a pagar</p>
              <p className="metric-card__value">
                {formatCurrency(report.totalWeeklyPayment)}
              </p>
            </div>
          </div>
        </div>
      </section>

      <div className="metric-grid">
        <SummaryCard
          title="Total por tipo de contrato"
          items={report.totalsByType}
          totalAmount={report.totalWeeklyPayment}
        />
        <SummaryCard
          title="Total por departamento"
          items={report.totalsByDepartment}
          totalAmount={report.totalWeeklyPayment}
        />
      </div>

      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Detalle por empleado</h2>
            <p className="card__description">
              Seleccione una fila para ver la formula y el desglose del calculo.
            </p>
          </div>
        </div>

        {report.lines.length === 0 ? (
          <EmptyState
            title="No hay empleados que incluir en el reporte"
            description="Registre empleados o desmarque el filtro de empleados activos."
          />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Empleado</th>
                  <th>Seguro social</th>
                  <th>Tipo de contrato</th>
                  <th>Departamento</th>
                  <th className="table th--numeric">Pago semanal</th>
                  <th aria-label="Detalle" />
                </tr>
              </thead>
              <tbody>
                {report.lines.map((line) => (
                  <PayrollRow
                    key={line.employeeId}
                    line={line}
                    isExpanded={expandedEmployeeId === line.employeeId}
                    onToggle={() =>
                      setExpandedEmployeeId(
                        expandedEmployeeId === line.employeeId ? null : line.employeeId,
                      )
                    }
                  />
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={4}>Total general</th>
                  <th className="table th--numeric">
                    {formatCurrency(report.totalWeeklyPayment)}
                  </th>
                  <th />
                </tr>
              </tfoot>
            </table>
          </div>
        )}
      </section>
    </>
  );
}

function PayrollRow({
  line,
  isExpanded,
  onToggle,
}: {
  line: PayrollReportLine;
  isExpanded: boolean;
  onToggle: () => void;
}) {
  return (
    <>
      <tr
        {...buildClickableRowProps(
          onToggle,
          isExpanded
            ? `Ocultar el calculo de ${line.fullName}`
            : `Ver el calculo de ${line.fullName}`,
        )}
      >
        <td>{line.fullName}</td>
        <td>{line.socialSecurityNumber}</td>
        <td>
          <span className="badge badge--type">{line.typeDescription}</span>
        </td>
        <td>{line.departmentName}</td>
        <td className="table td--numeric">{formatCurrency(line.weeklyPayment)}</td>
        <td>
          <button type="button" className="button button--secondary" onClick={onToggle}>
            {isExpanded ? 'Ocultar' : 'Ver calculo'}
          </button>
        </td>
      </tr>
      {isExpanded && (
        <tr>
          <td colSpan={6} className="table td--wrap">
            <code className="formula">{line.paymentBreakdown.formula}</code>
            <ul className="breakdown" style={{ marginTop: 12 }}>
              {line.paymentBreakdown.components.map((component) => (
                <li className="breakdown__item" key={component.concept}>
                  <span>
                    <span className="breakdown__concept">{component.concept}</span>{' '}
                    <span className="breakdown__detail">({component.detail})</span>
                  </span>
                  <span className="breakdown__amount">
                    {formatCurrency(component.amount)}
                  </span>
                </li>
              ))}
            </ul>
          </td>
        </tr>
      )}
    </>
  );
}

function SummaryCard({
  title,
  items,
  totalAmount,
}: {
  title: string;
  items: { groupName: string; employeeCount: number; totalWeeklyPayment: number }[];
  totalAmount: number;
}) {
  return (
    <section className="card">
      <h2 className="card__title">{title}</h2>
      <div style={{ marginTop: 12 }}>
        {items.length === 0 && <p className="card__description">Sin informacion.</p>}
        {items.map((item) => {
          const percentage =
            totalAmount === 0 ? 0 : (item.totalWeeklyPayment / totalAmount) * 100;

          return (
            <div className="detail-row" key={item.groupName}>
              <span className="detail-row__label">
                {item.groupName}
                <br />
                <span className="field__hint">
                  {item.employeeCount} empleado(s) - {percentage.toFixed(1)}%
                </span>
              </span>
              <span className="detail-row__value">
                {formatCurrency(item.totalWeeklyPayment)}
              </span>
            </div>
          );
        })}
      </div>
    </section>
  );
}
