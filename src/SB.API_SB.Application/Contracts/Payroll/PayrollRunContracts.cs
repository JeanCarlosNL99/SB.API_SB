using SB.API_SB.Application.Common;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Contracts.Payroll;

/// <summary>Datos necesarios para generar la nomina de una semana.</summary>
public sealed class GeneratePayrollRunRequest
{
    /// <summary>Compania cuya nomina se va a calcular.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Ano ISO 8601 de la semana a pagar.</summary>
    public int Year { get; set; }

    /// <summary>Numero de semana ISO 8601 a pagar.</summary>
    public int WeekNumber { get; set; }

    /// <summary>
    /// Indica si se incluyen solo los empleados activos. Por omision es verdadero:
    /// un empleado inactivo no genera pago.
    /// </summary>
    public bool OnlyActiveEmployees { get; set; } = true;
}

/// <summary>Motivo con el que se anula una ejecucion de nomina.</summary>
public sealed class CancelPayrollRunRequest
{
    /// <summary>Explicacion de por que se anula la ejecucion.</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Filtros del historial de ejecuciones de nomina.</summary>
public sealed class PayrollRunFilterRequest : PaginationRequest
{
    /// <summary>Compania por la que se filtra.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Ano por el que se filtra.</summary>
    public int? Year { get; set; }

    /// <summary>Indica si se incluyen las ejecuciones anuladas.</summary>
    public bool IncludeCancelled { get; set; } = true;
}

/// <summary>Cabecera de una ejecucion de nomina, para el listado del historial.</summary>
public sealed class PayrollRunSummaryResponse
{
    /// <summary>Identificador de la ejecucion.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador de la compania pagada.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Razon social de la compania pagada.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Ano de la semana pagada.</summary>
    public int Year { get; set; }

    /// <summary>Numero de la semana pagada.</summary>
    public int WeekNumber { get; set; }

    /// <summary>Etiqueta legible de la semana, por ejemplo <c>2026-S35</c>.</summary>
    public string WeekLabel { get; set; } = string.Empty;

    /// <summary>Primer dia del periodo pagado.</summary>
    public DateOnly WeekStartDate { get; set; }

    /// <summary>Ultimo dia del periodo pagado.</summary>
    public DateOnly WeekEndDate { get; set; }

    /// <summary>Estado de la ejecucion.</summary>
    public PayrollRunStatus Status { get; set; }

    /// <summary>Descripcion legible del estado.</summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>Cantidad de empleados incluidos.</summary>
    public int EmployeeCount { get; set; }

    /// <summary>Monto total pagado.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Fecha y hora (UTC) en que se genero la ejecucion.</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>Usuario que genero la ejecucion.</summary>
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>Motivo de anulacion, cuando aplica.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Fecha y hora (UTC) de anulacion, cuando aplica.</summary>
    public DateTime? CancelledAt { get; set; }
}

/// <summary>Linea de una ejecucion de nomina.</summary>
public sealed class PayrollRunLineResponse
{
    /// <summary>Identificador de la linea.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador del empleado pagado, si aun existe.</summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>Nombre completo del empleado al momento del pago.</summary>
    public string EmployeeFullName { get; set; } = string.Empty;

    /// <summary>Numero de seguro social al momento del pago.</summary>
    public string SocialSecurityNumber { get; set; } = string.Empty;

    /// <summary>Tipo de contrato con el que se calculo el pago.</summary>
    public EmployeeType EmployeeType { get; set; }

    /// <summary>Descripcion legible del tipo de contrato.</summary>
    public string EmployeeTypeDescription { get; set; } = string.Empty;

    /// <summary>Departamento del empleado al momento del pago.</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>Monto pagado en la semana.</summary>
    public decimal WeeklyPayment { get; set; }

    /// <summary>Formula aplicada.</summary>
    public string PaymentFormula { get; set; } = string.Empty;

    /// <summary>Componentes que suman el monto pagado.</summary>
    public IReadOnlyCollection<PaymentComponentResponse> Components { get; set; } =
        Array.Empty<PaymentComponentResponse>();
}

/// <summary>Ejecucion de nomina con su detalle completo.</summary>
public sealed class PayrollRunDetailResponse
{
    /// <summary>Cabecera de la ejecucion.</summary>
    public PayrollRunSummaryResponse Summary { get; set; } = new();

    /// <summary>Detalle por empleado.</summary>
    public IReadOnlyCollection<PayrollRunLineResponse> Lines { get; set; } =
        Array.Empty<PayrollRunLineResponse>();

    /// <summary>Totales agrupados por tipo de contrato.</summary>
    public IReadOnlyCollection<PayrollSummaryItemResponse> TotalsByType { get; set; } =
        Array.Empty<PayrollSummaryItemResponse>();

    /// <summary>Totales agrupados por departamento.</summary>
    public IReadOnlyCollection<PayrollSummaryItemResponse> TotalsByDepartment { get; set; } =
        Array.Empty<PayrollSummaryItemResponse>();
}

/// <summary>
/// Vista previa del calculo de una semana antes de generarla. Tiene la misma
/// forma que el detalle para que la interfaz reutilice la misma vista, pero no
/// corresponde a ningun documento almacenado.
/// </summary>
public sealed class PayrollPreviewResponse
{
    /// <summary>Identificador de la compania.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Razon social de la compania.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Ano de la semana.</summary>
    public int Year { get; set; }

    /// <summary>Numero de semana.</summary>
    public int WeekNumber { get; set; }

    /// <summary>Etiqueta legible de la semana.</summary>
    public string WeekLabel { get; set; } = string.Empty;

    /// <summary>Primer dia del periodo.</summary>
    public DateOnly WeekStartDate { get; set; }

    /// <summary>Ultimo dia del periodo.</summary>
    public DateOnly WeekEndDate { get; set; }

    /// <summary>Cantidad de empleados que entrarian en la nomina.</summary>
    public int EmployeeCount { get; set; }

    /// <summary>Monto total que se pagaria.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Indica si la semana ya fue generada y por tanto no se puede repetir.</summary>
    public bool IsAlreadyGenerated { get; set; }

    /// <summary>Identificador de la ejecucion existente, cuando la semana ya fue pagada.</summary>
    public Guid? ExistingPayrollRunId { get; set; }

    /// <summary>Detalle por empleado del calculo propuesto.</summary>
    public IReadOnlyCollection<PayrollRunLineResponse> Lines { get; set; } =
        Array.Empty<PayrollRunLineResponse>();

    /// <summary>Totales agrupados por tipo de contrato.</summary>
    public IReadOnlyCollection<PayrollSummaryItemResponse> TotalsByType { get; set; } =
        Array.Empty<PayrollSummaryItemResponse>();

    /// <summary>Totales agrupados por departamento.</summary>
    public IReadOnlyCollection<PayrollSummaryItemResponse> TotalsByDepartment { get; set; } =
        Array.Empty<PayrollSummaryItemResponse>();
}

/// <summary>Semanas ya pagadas por una compania en un ano determinado.</summary>
public sealed class GeneratedWeeksResponse
{
    /// <summary>Identificador de la compania consultada.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Ano consultado.</summary>
    public int Year { get; set; }

    /// <summary>Cantidad total de semanas que tiene el ano.</summary>
    public int WeeksInYear { get; set; }

    /// <summary>Numeros de semana con nomina vigente.</summary>
    public IReadOnlyCollection<int> GeneratedWeekNumbers { get; set; } = Array.Empty<int>();
}
