using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Application.Interfaces.Payroll;

/// <summary>
/// Construye las lineas de una nomina a partir de los empleados que la integran.
/// </summary>
/// <remarks>
/// Se declara como contrato para que la vista previa y la generacion dependan de
/// la misma abstraccion, y para poder verificar la construccion de las lineas en
/// pruebas unitarias sin base de datos.
/// </remarks>
public interface IPayrollCalculator
{
    /// <summary>Construye la instantanea de pago de cada empleado.</summary>
    /// <param name="employees">Empleados a incluir en la nomina.</param>
    /// <param name="payrollWeek">Semana que se esta calculando.</param>
    /// <returns>Lineas de nomina, ordenadas por apellido.</returns>
    IReadOnlyCollection<PayrollRunLine> BuildLines(
        IReadOnlyCollection<Employee> employees,
        PayrollWeek payrollWeek);
}
