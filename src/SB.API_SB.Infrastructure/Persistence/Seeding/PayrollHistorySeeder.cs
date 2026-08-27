using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Domain.Constants;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Infrastructure.Persistence.Seeding;

/// <summary>
/// Siembra un historial de nominas de semanas anteriores, por entidad
/// gubernamental, para que el modulo de reportes tenga datos con los que trabajar
/// desde el primer arranque.
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
    private const string UNKNOWN_GOVERNMENT_ENTITY_NAME = "Entidad no disponible";

    /// <summary>Cantidad de semanas anteriores que se siembran como historial.</summary>
    public const int WEEKS_OF_HISTORY = 8;

    private readonly ApplicationDbContext databaseContext;
    private readonly IGovernmentEntityRepository governmentEntityRepository;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ILogger<PayrollHistorySeeder> logger;

    public PayrollHistorySeeder(
        ApplicationDbContext databaseContext,
        IGovernmentEntityRepository governmentEntityRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<PayrollHistorySeeder> logger)
    {
        this.databaseContext = databaseContext;
        this.governmentEntityRepository = governmentEntityRepository;
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

        // Los empleados se agrupan por entidad gubernamental aqui y no con una
        // consulta que las una: la entidad vive en el archivo de texto plano y no
        // hay ninguna union posible entre los dos almacenes.
        List<Employee> employees = await databaseContext.Employees
            .Include(employee => employee.Department)
            .Where(employee => employee.Status == EmployeeStatus.Active)
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return;
        }

        IReadOnlyDictionary<Guid, string> governmentEntityNames =
            await governmentEntityRepository.GetNamesByIdentifierAsync(cancellationToken);

        List<IGrouping<Guid, Employee>> employeesByEntity = employees
            .GroupBy(employee => employee.GovernmentEntityId)
            .ToList();

        // La semana en curso se deja sin generar a proposito: es la que el usuario
        // va a calcular para probar el flujo completo.
        PayrollWeek currentWeek = PayrollWeek.Current(dateTimeProvider.UtcNow);
        List<PayrollRun> payrollRuns = new();

        foreach (IGrouping<Guid, Employee> entityEmployees in employeesByEntity)
        {
            string governmentEntityName = governmentEntityNames.TryGetValue(
                entityEmployees.Key,
                out string? resolvedName)
                ? resolvedName
                : UNKNOWN_GOVERNMENT_ENTITY_NAME;

            List<Employee> entityEmployeeList = entityEmployees.ToList();
            PayrollWeek week = currentWeek.Previous();

            for (int weekIndex = 0; weekIndex < WEEKS_OF_HISTORY; weekIndex++)
            {
                payrollRuns.Add(BuildPayrollRun(
                    entityEmployees.Key,
                    governmentEntityName,
                    entityEmployeeList,
                    week));

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
            "Se sembraron {PayrollRunCount} nominas historicas para " +
            "{GovernmentEntityCount} entidad(es) gubernamental(es), cubriendo las " +
            "{WeeksOfHistory} semanas anteriores a {CurrentWeek}.",
            payrollRuns.Count,
            employeesByEntity.Count,
            WEEKS_OF_HISTORY,
            currentWeek.Label);
    }

    private PayrollRun BuildPayrollRun(
        Guid governmentEntityId,
        string governmentEntityName,
        IReadOnlyCollection<Employee> employees,
        PayrollWeek week)
    {
        DateTime generatedAt = week.EndDate
            .ToDateTime(new TimeOnly(hour: 18, minute: 0))
            .ToUniversalTime();

        PayrollRun payrollRun = new()
        {
            GovernmentEntityId = governmentEntityId,
            GovernmentEntityName = governmentEntityName,
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
