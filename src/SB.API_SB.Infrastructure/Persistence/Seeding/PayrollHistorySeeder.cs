using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Domain.Constants;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Infrastructure.Persistence.Seeding;

/// <summary>
/// Siembra un historial de nominas de semanas anteriores para que el modulo de
/// reportes tenga datos con los que trabajar desde el primer arranque.
/// </summary>
/// <remarks>
/// Las semanas historicas no se calculan con los datos vigentes de cada empleado:
/// se aplica una variacion determinista por semana a las horas trabajadas y a las
/// ventas, de modo que el historial refleje lo que realmente ocurre en una nomina
/// real, donde cada semana es distinta de la anterior. La variacion se deriva del
/// numero de semana y no de un generador aleatorio, para que el sembrado sea
/// reproducible.
/// </remarks>
public sealed class PayrollHistorySeeder
{
    private const string SEED_USER_NAME = "Semilla";

    /// <summary>Cantidad de semanas anteriores que se siembran como historial.</summary>
    public const int WEEKS_OF_HISTORY = 8;

    private readonly ApplicationDbContext databaseContext;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ILogger<PayrollHistorySeeder> logger;

    public PayrollHistorySeeder(
        ApplicationDbContext databaseContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<PayrollHistorySeeder> logger)
    {
        this.databaseContext = databaseContext;
        this.dateTimeProvider = dateTimeProvider;
        this.logger = logger;
    }

    /// <summary>
    /// Genera el historial si todavia no existe ninguna nomina. La comprobacion
    /// hace la operacion idempotente: reiniciar la aplicacion no duplica el
    /// historial.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        bool payrollRunsExist = await databaseContext.PayrollRuns.AnyAsync(cancellationToken);

        if (payrollRunsExist)
        {
            return;
        }

        List<Company> companies = await databaseContext.Companies
            .Include(company => company.Employees)
                .ThenInclude(employee => employee.Department)
            .ToListAsync(cancellationToken);

        if (companies.Count == 0)
        {
            return;
        }

        // La semana en curso se deja sin generar a proposito: es la que el usuario
        // va a calcular para probar el flujo completo.
        PayrollWeek currentWeek = PayrollWeek.Current(dateTimeProvider.UtcNow);
        List<PayrollRun> payrollRuns = new();

        foreach (Company company in companies)
        {
            List<Employee> activeEmployees = company.Employees
                .Where(employee => employee.Status == EmployeeStatus.Active)
                .ToList();

            if (activeEmployees.Count == 0)
            {
                continue;
            }

            PayrollWeek week = currentWeek.Previous();

            for (int weekIndex = 0; weekIndex < WEEKS_OF_HISTORY; weekIndex++)
            {
                payrollRuns.Add(BuildPayrollRun(company, activeEmployees, week));

                week = week.Previous();
            }
        }

        if (payrollRuns.Count == 0)
        {
            return;
        }

        databaseContext.PayrollRuns.AddRange(payrollRuns);

        await databaseContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Se sembraron {PayrollRunCount} nominas historicas para {CompanyCount} compania(s), " +
            "cubriendo las {WeeksOfHistory} semanas anteriores a {CurrentWeek}.",
            payrollRuns.Count,
            companies.Count,
            WEEKS_OF_HISTORY,
            currentWeek.Label);
    }

    private PayrollRun BuildPayrollRun(
        Company company,
        IReadOnlyCollection<Employee> employees,
        PayrollWeek week)
    {
        DateTime generatedAt = week.EndDate
            .ToDateTime(new TimeOnly(hour: 18, minute: 0))
            .ToUniversalTime();

        PayrollRun payrollRun = new()
        {
            CompanyId = company.Id,
            Status = PayrollRunStatus.Generated,
            CreatedAt = generatedAt,
            CreatedBy = SEED_USER_NAME
        };

        payrollRun.AssignPayrollWeek(week);

        foreach (Employee employee in employees)
        {
            payrollRun.Lines.Add(BuildLine(employee, week, generatedAt));
        }

        payrollRun.RecalculateTotals();

        return payrollRun;
    }

    private static PayrollRunLine BuildLine(
        Employee employee,
        PayrollWeek week,
        DateTime generatedAt)
    {
        // Se trabaja sobre una copia del empleado con los valores variados de la
        // semana, para no alterar los datos vigentes al calcular el historico.
        Employee weeklyEmployee = CloneWithWeeklyVariation(employee, week);

        var breakdown = weeklyEmployee.BuildPaymentBreakdown();

        PayrollRunLine line = new()
        {
            EmployeeId = employee.Id,
            EmployeeFullName = employee.FullName,
            SocialSecurityNumber = employee.SocialSecurityNumber,
            EmployeeType = employee.Type,
            EmployeeTypeDescription = DescribeType(employee.Type),
            DepartmentName = employee.Department?.Name ?? "Sin departamento",
            WeeklyPayment = breakdown.TotalAmount,
            PaymentFormula = breakdown.Formula,
            CreatedAt = generatedAt,
            CreatedBy = SEED_USER_NAME
        };

        int sortOrder = 0;

        foreach (var component in breakdown.Components)
        {
            line.Components.Add(new PayrollRunLineComponent
            {
                SortOrder = sortOrder++,
                Concept = component.Concept,
                Detail = component.Detail,
                Amount = component.Amount,
                CreatedAt = generatedAt,
                CreatedBy = SEED_USER_NAME
            });
        }

        return line;
    }

    /// <summary>
    /// Crea una copia del empleado con los valores variables de la semana
    /// ajustados. El salario fijo no varia; si varian las horas trabajadas y las
    /// ventas, que es lo que cambia semana a semana en la realidad.
    /// </summary>
    /// <param name="employee">Empleado de origen.</param>
    /// <param name="week">Semana que se esta sembrando.</param>
    /// <returns>Copia con los valores de esa semana.</returns>
    private static Employee CloneWithWeeklyVariation(Employee employee, PayrollWeek week)
    {
        // Factor deterministico entre 0.85 y 1.15 derivado del numero de semana.
        decimal variationFactor = 1m + ((week.WeekNumber % 7) - 3) * 0.05m;

        return employee switch
        {
            SalariedEmployee salaried => new SalariedEmployee
            {
                WeeklySalary = salaried.WeeklySalary
            },
            HourlyEmployee hourly => new HourlyEmployee
            {
                HourlyWage = hourly.HourlyWage,
                HoursWorked = RoundHours(
                    Math.Min(
                        hourly.HoursWorked * variationFactor,
                        PayrollConstants.MAXIMUM_WEEKLY_HOURS))
            },
            BaseSalariedCommissionEmployee baseSalaried => new BaseSalariedCommissionEmployee
            {
                GrossSales = RoundCurrency(baseSalaried.GrossSales * variationFactor),
                CommissionRate = baseSalaried.CommissionRate,
                BaseSalary = baseSalaried.BaseSalary
            },
            CommissionEmployee commission => new CommissionEmployee
            {
                GrossSales = RoundCurrency(commission.GrossSales * variationFactor),
                CommissionRate = commission.CommissionRate
            },
            _ => employee
        };
    }

    private static decimal RoundHours(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal RoundCurrency(decimal value) =>
        Math.Round(value, PayrollConstants.CURRENCY_DECIMAL_PLACES, MidpointRounding.AwayFromZero);

    private static string DescribeType(EmployeeType employeeType) => employeeType switch
    {
        EmployeeType.Salaried => "Empleado asalariado",
        EmployeeType.Hourly => "Empleado por horas",
        EmployeeType.Commission => "Empleado por comision",
        EmployeeType.BaseSalariedCommission => "Empleado asalariado por comision",
        _ => employeeType.ToString()
    };
}
