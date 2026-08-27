using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Application.Mappings;

/// <summary>Proyecciones del historial de nomina hacia sus contratos publicos.</summary>
public static class PayrollRunMappings
{
    /// <summary>Convierte la cabecera de una ejecucion en su respuesta de API.</summary>
    /// <param name="payrollRun">Ejecucion de nomina.</param>
    /// <returns>Cabecera lista para devolverse.</returns>
    public static PayrollRunSummaryResponse ToSummaryResponse(this PayrollRun payrollRun)
    {
        ArgumentNullException.ThrowIfNull(payrollRun);

        return new PayrollRunSummaryResponse
        {
            Id = payrollRun.Id,
            GovernmentEntityId = payrollRun.GovernmentEntityId,
            GovernmentEntityName = payrollRun.GovernmentEntityName,
            Year = payrollRun.Year,
            WeekNumber = payrollRun.WeekNumber,
            WeekLabel = payrollRun.GetPayrollWeek().Label,
            WeekStartDate = payrollRun.WeekStartDate,
            WeekEndDate = payrollRun.WeekEndDate,
            Status = payrollRun.Status,
            StatusDescription = payrollRun.Status.Describe(),
            EmployeeCount = payrollRun.EmployeeCount,
            TotalAmount = payrollRun.TotalAmount,
            GeneratedAt = payrollRun.CreatedAt,
            GeneratedBy = payrollRun.CreatedBy,
            CancellationReason = payrollRun.CancellationReason,
            CancelledAt = payrollRun.CancelledAt
        };
    }

    /// <summary>Convierte una linea de nomina en su respuesta de API.</summary>
    /// <param name="line">Linea de la ejecucion.</param>
    /// <returns>Linea lista para devolverse.</returns>
    public static PayrollRunLineResponse ToResponse(this PayrollRunLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return new PayrollRunLineResponse
        {
            Id = line.Id,
            EmployeeId = line.EmployeeId,
            EmployeeFullName = line.EmployeeFullName,
            SocialSecurityNumber = line.SocialSecurityNumber,
            EmployeeType = line.EmployeeType,
            EmployeeTypeDescription = line.EmployeeTypeDescription,
            DepartmentName = line.DepartmentName,
            WeeklyPayment = line.WeeklyPayment,
            PaymentFormula = line.PaymentFormula,
            Components = line.Components
                .OrderBy(component => component.SortOrder)
                .Select(component => new PaymentComponentResponse
                {
                    Concept = component.Concept,
                    Detail = component.Detail,
                    Amount = component.Amount
                })
                .ToList()
        };
    }

    /// <summary>Convierte una ejecucion completa en su respuesta de detalle.</summary>
    /// <param name="payrollRun">Ejecucion con sus lineas cargadas.</param>
    /// <returns>Detalle listo para devolverse.</returns>
    public static PayrollRunDetailResponse ToDetailResponse(this PayrollRun payrollRun)
    {
        ArgumentNullException.ThrowIfNull(payrollRun);

        List<PayrollRunLineResponse> lines = payrollRun.Lines
            .OrderBy(line => line.EmployeeFullName, StringComparer.CurrentCultureIgnoreCase)
            .Select(line => line.ToResponse())
            .ToList();

        return new PayrollRunDetailResponse
        {
            Summary = payrollRun.ToSummaryResponse(),
            Lines = lines,
            TotalsByType = SummarizeBy(lines, line => line.EmployeeTypeDescription),
            TotalsByDepartment = SummarizeBy(lines, line => line.DepartmentName)
        };
    }

    /// <summary>
    /// Agrupa las lineas y totaliza el monto por grupo. Se comparte entre el
    /// detalle almacenado y la vista previa para que ambos muestren los mismos
    /// agregados calculados de la misma forma.
    /// </summary>
    /// <param name="lines">Lineas a agrupar.</param>
    /// <param name="groupSelector">Criterio de agrupamiento.</param>
    /// <returns>Totales por grupo, del mayor al menor.</returns>
    public static IReadOnlyCollection<PayrollSummaryItemResponse> SummarizeBy(
        IEnumerable<PayrollRunLineResponse> lines,
        Func<PayrollRunLineResponse, string> groupSelector)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(groupSelector);

        return lines
            .GroupBy(groupSelector)
            .Select(group => new PayrollSummaryItemResponse
            {
                GroupName = group.Key,
                EmployeeCount = group.Count(),
                TotalWeeklyPayment = group.Sum(line => line.WeeklyPayment)
            })
            .OrderByDescending(summary => summary.TotalWeeklyPayment)
            .ToList();
    }
}
