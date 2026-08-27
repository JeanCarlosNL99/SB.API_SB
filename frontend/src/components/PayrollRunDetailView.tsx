import { PayrollLinesTable } from './PayrollLinesTable';
import { formatCurrency, formatDate, formatDateTime } from '@/utils/formatters';
import type { PayrollRunDetail, PayrollSummaryItem } from '@/types/api';

/**
 * Vista completa de una nomina generada: cabecera, totales agregados y detalle
 * por empleado. Se comparte entre el resultado de la generacion y el detalle del
 * historial.
 */
export function PayrollRunDetailView({ detail }: { detail: PayrollRunDetail }) {
  const { summary } = detail;

  return (
    <div>
      <div className="detail-row">
        <span className="detail-row__label">Compania</span>
        <span className="detail-row__value">{summary.companyName}</span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Semana pagada</span>
        <span className="detail-row__value">
          {summary.weekLabel} ({formatDate(summary.weekStartDate)} al{' '}
          {formatDate(summary.weekEndDate)})
        </span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Estado</span>
        <span className="detail-row__value">
          <span
            className={
              summary.status === 'Generated' ? 'badge badge--active' : 'badge badge--inactive'
            }
          >
            {summary.statusDescription}
          </span>
        </span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Generada</span>
        <span className="detail-row__value">
          {formatDateTime(summary.generatedAt)} por {summary.generatedBy}
        </span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Empleados pagados</span>
        <span className="detail-row__value">{summary.employeeCount}</span>
      </div>
      <div className="detail-row">
        <span className="detail-row__label">Total pagado</span>
        <span className="detail-row__value">{formatCurrency(summary.totalAmount)}</span>
      </div>

      {summary.status === 'Cancelled' && (
        <div className="alert alert--error" style={{ marginTop: 16 }} role="status">
          <div>
            <strong>Ejecucion anulada el {formatDateTime(summary.cancelledAt)}.</strong>
            <p style={{ marginTop: 4 }}>{summary.cancellationReason}</p>
          </div>
        </div>
      )}

      <div style={{ marginTop: 20 }}>
        <p className="section-title">Totales por tipo de contrato</p>
        <SummaryList items={detail.totalsByType} />
      </div>

      <div style={{ marginTop: 16 }}>
        <p className="section-title">Totales por departamento</p>
        <SummaryList items={detail.totalsByDepartment} />
      </div>

      <div style={{ marginTop: 20 }}>
        <p className="section-title">Detalle por empleado</p>
        <PayrollLinesTable lines={detail.lines} totalAmount={summary.totalAmount} />
      </div>
    </div>
  );
}

function SummaryList({ items }: { items: PayrollSummaryItem[] }) {
  if (items.length === 0) {
    return <p className="card__description">Sin informacion.</p>;
  }

  return (
    <ul className="breakdown">
      {items.map((item) => (
        <li className="breakdown__item" key={item.groupName}>
          <span>
            <span className="breakdown__concept">{item.groupName}</span>{' '}
            <span className="breakdown__detail">({item.employeeCount} empleado(s))</span>
          </span>
          <span className="breakdown__amount">
            {formatCurrency(item.totalWeeklyPayment)}
          </span>
        </li>
      ))}
    </ul>
  );
}
