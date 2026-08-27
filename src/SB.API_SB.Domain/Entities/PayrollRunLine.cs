using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Linea de una ejecucion de nomina: lo que se le pago a un empleado en una
/// semana concreta.
/// </summary>
/// <remarks>
/// Los datos del empleado se copian aqui a proposito. Es una instantanea: si el
/// empleado cambia de departamento o se elimina, el documento historico debe
/// seguir mostrando a quien se le pago y bajo que condiciones.
/// </remarks>
public sealed class PayrollRunLine : AuditableEntity
{
    /// <summary>Ejecucion de nomina a la que pertenece la linea.</summary>
    public Guid PayrollRunId { get; set; }

    /// <summary>Ejecucion de nomina a la que pertenece la linea.</summary>
    public PayrollRun? PayrollRun { get; set; }

    /// <summary>
    /// Empleado al que corresponde el pago. Es opcional porque el empleado puede
    /// eliminarse despues sin que se pierda el historico de lo pagado.
    /// </summary>
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

    /// <summary>Formula aplicada, conservada como texto para poder auditarla.</summary>
    public string PaymentFormula { get; set; } = string.Empty;

    /// <summary>Componentes que suman el monto pagado.</summary>
    public ICollection<PayrollRunLineComponent> Components { get; set; } =
        new List<PayrollRunLineComponent>();
}
