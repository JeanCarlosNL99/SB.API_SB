using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Ejecucion de nomina de una compania para una semana determinada.
/// </summary>
/// <remarks>
/// Una ejecucion es un documento historico, no una consulta. Sus lineas guardan
/// el monto y el detalle del calculo tal como quedaron el dia en que se genero:
/// si manana cambian las horas trabajadas de un empleado, la nomina de la semana
/// pasada debe seguir mostrando lo que realmente se pago. Recalcular el historico
/// a partir de los datos vigentes seria un error, no una optimizacion.
/// </remarks>
public sealed class PayrollRun : AuditableEntity
{
    /// <summary>Compania a la que corresponde la nomina.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Compania a la que corresponde la nomina.</summary>
    public Company? Company { get; set; }

    /// <summary>Ano ISO 8601 de la semana pagada.</summary>
    public int Year { get; set; }

    /// <summary>Numero de semana ISO 8601 pagada.</summary>
    public int WeekNumber { get; set; }

    /// <summary>Primer dia del periodo pagado.</summary>
    public DateOnly WeekStartDate { get; set; }

    /// <summary>Ultimo dia del periodo pagado.</summary>
    public DateOnly WeekEndDate { get; set; }

    /// <summary>Estado de la ejecucion.</summary>
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Generated;

    /// <summary>Cantidad de empleados incluidos en la ejecucion.</summary>
    public int EmployeeCount { get; set; }

    /// <summary>Monto total pagado en la semana.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Motivo registrado al anular la ejecucion.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Fecha y hora (UTC) en que se anulo la ejecucion.</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>Detalle por empleado.</summary>
    public ICollection<PayrollRunLine> Lines { get; set; } = new List<PayrollRunLine>();

    /// <summary>Semana de nomina que representa esta ejecucion.</summary>
    public PayrollWeek GetPayrollWeek() => PayrollWeek.Create(Year, WeekNumber);

    /// <summary>
    /// Aplica al documento la semana indicada, dejando ano, numero y rango de
    /// fechas siempre coherentes entre si.
    /// </summary>
    /// <param name="payrollWeek">Semana a registrar.</param>
    public void AssignPayrollWeek(PayrollWeek payrollWeek)
    {
        ArgumentNullException.ThrowIfNull(payrollWeek);

        Year = payrollWeek.Year;
        WeekNumber = payrollWeek.WeekNumber;
        WeekStartDate = payrollWeek.StartDate;
        WeekEndDate = payrollWeek.EndDate;
    }

    /// <summary>Recalcula los totales a partir de las lineas cargadas.</summary>
    public void RecalculateTotals()
    {
        EmployeeCount = Lines.Count;
        TotalAmount = Lines.Sum(line => line.WeeklyPayment);
    }
}
