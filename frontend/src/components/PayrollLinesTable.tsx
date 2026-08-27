import { useState } from 'react';
import { formatCurrency } from '@/utils/formatters';
import { buildClickableRowProps } from '@/utils/tableInteraction';
import type { PayrollRunLine } from '@/types/api';

/**
 * Tabla del detalle de pago por empleado, con el desglose desplegable.
 *
 * Se extrae a un componente porque la usan tres pantallas: la vista previa del
 * calculo, la nomina recien generada y el detalle del historial. Las tres deben
 * mostrar exactamente la misma informacion de la misma forma.
 */
export function PayrollLinesTable({
  lines,
  totalAmount,
}: {
  lines: PayrollRunLine[];
  totalAmount: number;
}) {
  const [expandedLineId, setExpandedLineId] = useState<string | null>(null);

  return (
    <div className="table-wrapper">
      <table className="table">
        <thead>
          <tr>
            <th>Empleado</th>
            <th>Seguro social</th>
            <th>Tipo de contrato</th>
            <th>Departamento</th>
            <th className="table th--numeric">Pago de la semana</th>
            <th aria-label="Detalle" />
          </tr>
        </thead>
        <tbody>
          {lines.map((line) => {
            const isExpanded = expandedLineId === line.id;
            const toggle = () => setExpandedLineId(isExpanded ? null : line.id);

            return (
              <PayrollLineRow
                key={line.id}
                line={line}
                isExpanded={isExpanded}
                onToggle={toggle}
              />
            );
          })}
        </tbody>
        <tfoot>
          <tr>
            <th colSpan={4}>Total de la nomina</th>
            <th className="table th--numeric">{formatCurrency(totalAmount)}</th>
            <th />
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

function PayrollLineRow({
  line,
  isExpanded,
  onToggle,
}: {
  line: PayrollRunLine;
  isExpanded: boolean;
  onToggle: () => void;
}) {
  return (
    <>
      <tr
        {...buildClickableRowProps(
          onToggle,
          isExpanded
            ? `Ocultar el calculo de ${line.employeeFullName}`
            : `Ver el calculo de ${line.employeeFullName}`,
        )}
      >
        <td>{line.employeeFullName}</td>
        <td>{line.socialSecurityNumber}</td>
        <td>
          <span className="badge badge--type">{line.employeeTypeDescription}</span>
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
            <code className="formula">{line.paymentFormula}</code>
            <ul className="breakdown" style={{ marginTop: 12 }}>
              {line.components.map((component) => (
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
