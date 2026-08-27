using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.Payroll;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Payroll;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Application.Mappings;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Services.Payroll;

/// <summary>
/// Implementacion del calculo de pagos semanales por entidad gubernamental.
/// </summary>
/// <remarks>
/// La regla central del modulo es que una semana se paga una sola vez. Se aplica
/// en dos niveles: aqui, con una comprobacion que produce un mensaje util para el
/// usuario, y en la base de datos, con un indice unico filtrado que cierra la
/// ventana de una condicion de carrera entre dos peticiones simultaneas. La
/// comprobacion del servicio sin el indice seria una ilusion de seguridad.
/// </remarks>
public sealed class PayrollRunService : IPayrollRunService
{
    private const string PAYROLL_RUN_ENTITY_NAME = "la ejecucion de nomina";
    private const string GOVERNMENT_ENTITY_NAME = "la entidad gubernamental";
    private const string UNKNOWN_GOVERNMENT_ENTITY_NAME = "Entidad no disponible";

    private readonly IPayrollRunRepository payrollRunRepository;
    private readonly IGovernmentEntityRepository governmentEntityRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IPayrollCalculator payrollCalculator;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<PayrollRunService> logger;

    public PayrollRunService(
        IPayrollRunRepository payrollRunRepository,
        IGovernmentEntityRepository governmentEntityRepository,
        IEmployeeRepository employeeRepository,
        IPayrollCalculator payrollCalculator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<PayrollRunService> logger)
    {
        this.payrollRunRepository = payrollRunRepository;
        this.governmentEntityRepository = governmentEntityRepository;
        this.employeeRepository = employeeRepository;
        this.payrollCalculator = payrollCalculator;
        this.dateTimeProvider = dateTimeProvider;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PayrollPreviewResponse> PreviewAsync(
        Guid governmentEntityId,
        int year,
        int weekNumber,
        bool onlyActiveEmployees,
        CancellationToken cancellationToken = default)
    {
        GovernmentEntity governmentEntity = await GetRequiredGovernmentEntityAsync(
            governmentEntityId,
            cancellationToken);
        PayrollWeek payrollWeek = PayrollWeek.Create(year, weekNumber);

        PayrollRun? existingRun = await payrollRunRepository.FindGeneratedRunAsync(
            governmentEntityId,
            payrollWeek,
            cancellationToken);

        IReadOnlyCollection<PayrollRunLine> lines = await BuildLinesAsync(
            governmentEntityId,
            payrollWeek,
            onlyActiveEmployees,
            cancellationToken);

        List<PayrollRunLineResponse> lineResponses = lines
            .Select(line => line.ToResponse())
            .ToList();

        logger.LogInformation(
            "Vista previa de nomina para {GovernmentEntityName}, semana {WeekLabel}. " +
            "Empleados: {EmployeeCount}. Total: {TotalAmount}. Ya generada: {IsAlreadyGenerated}.",
            governmentEntity.Name,
            payrollWeek.Label,
            lineResponses.Count,
            lineResponses.Sum(line => line.WeeklyPayment),
            existingRun is not null);

        return new PayrollPreviewResponse
        {
            GovernmentEntityId = governmentEntity.Id,
            GovernmentEntityName = governmentEntity.Name,
            Year = payrollWeek.Year,
            WeekNumber = payrollWeek.WeekNumber,
            WeekLabel = payrollWeek.Label,
            WeekStartDate = payrollWeek.StartDate,
            WeekEndDate = payrollWeek.EndDate,
            EmployeeCount = lineResponses.Count,
            TotalAmount = lineResponses.Sum(line => line.WeeklyPayment),
            IsAlreadyGenerated = existingRun is not null,
            ExistingPayrollRunId = existingRun?.Id,
            Lines = lineResponses,
            TotalsByType = PayrollRunMappings.SummarizeBy(
                lineResponses,
                line => line.EmployeeTypeDescription),
            TotalsByDepartment = PayrollRunMappings.SummarizeBy(
                lineResponses,
                line => line.DepartmentName)
        };
    }

    /// <inheritdoc />
    public async Task<PayrollRunDetailResponse> GenerateAsync(
        GeneratePayrollRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        GovernmentEntity governmentEntity = await GetRequiredGovernmentEntityAsync(
            request.GovernmentEntityId,
            cancellationToken);

        if (governmentEntity.Status != RecordStatus.Active)
        {
            throw new BusinessRuleViolationException(
                $"La entidad gubernamental '{governmentEntity.Name}' esta inactiva y no " +
                "admite generacion de nomina.");
        }

        PayrollWeek payrollWeek = PayrollWeek.Create(request.Year, request.WeekNumber);

        // Una semana que todavia no ha terminado no se puede pagar: las horas
        // trabajadas y las ventas de esa semana aun no estan completas.
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        if (payrollWeek.StartDate > today)
        {
            throw new BusinessRuleViolationException(
                $"La semana {payrollWeek.Label} todavia no ha comenzado " +
                $"(inicia el {payrollWeek.StartDate:dd/MM/yyyy}). No se puede generar su nomina.");
        }

        PayrollRun? existingRun = await payrollRunRepository.FindGeneratedRunAsync(
            request.GovernmentEntityId,
            payrollWeek,
            cancellationToken);

        if (existingRun is not null)
        {
            logger.LogWarning(
                "Se rechazo la generacion de nomina de {GovernmentEntityName} para la semana " +
                "{WeekLabel}: ya existe la ejecucion {ExistingPayrollRunId}.",
                governmentEntity.Name,
                payrollWeek.Label,
                existingRun.Id);

            throw new DuplicatedPayrollRunException(
                governmentEntity.Name,
                payrollWeek.Year,
                payrollWeek.WeekNumber,
                existingRun.Id);
        }

        IReadOnlyCollection<PayrollRunLine> lines = await BuildLinesAsync(
            request.GovernmentEntityId,
            payrollWeek,
            request.OnlyActiveEmployees,
            cancellationToken);

        if (lines.Count == 0)
        {
            throw new BusinessRuleViolationException(
                $"La entidad gubernamental '{governmentEntity.Name}' no tiene empleados " +
                $"que incluir en la nomina de la semana {payrollWeek.Label}.");
        }

        PayrollRun payrollRun = new()
        {
            GovernmentEntityId = governmentEntity.Id,
            GovernmentEntityName = governmentEntity.Name,
            Status = PayrollRunStatus.Generated
        };

        payrollRun.AssignPayrollWeek(payrollWeek);

        foreach (PayrollRunLine line in lines)
        {
            payrollRun.Lines.Add(line);
        }

        payrollRun.RecalculateTotals();

        await payrollRunRepository.AddAsync(payrollRun, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Nomina generada para {GovernmentEntityName}, semana {WeekLabel}. " +
            "Ejecucion: {PayrollRunId}. Empleados: {EmployeeCount}. " +
            "Total: {TotalAmount}.",
            governmentEntity.Name,
            payrollWeek.Label,
            payrollRun.Id,
            payrollRun.EmployeeCount,
            payrollRun.TotalAmount);

        return await GetByIdAsync(payrollRun.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PayableGovernmentEntityResponse>>
        GetPayableEntitiesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<GovernmentEntityEmployeeCount> employeeCounts =
            await employeeRepository.CountByGovernmentEntityAsync(cancellationToken);

        if (employeeCounts.Count == 0)
        {
            return Array.Empty<PayableGovernmentEntityResponse>();
        }

        IReadOnlyDictionary<Guid, string> governmentEntityNames =
            await governmentEntityRepository.GetNamesByIdentifierAsync(cancellationToken);

        return employeeCounts
            .Select(employeeCount => new PayableGovernmentEntityResponse
            {
                Id = employeeCount.GovernmentEntityId,
                Name = governmentEntityNames.TryGetValue(
                    employeeCount.GovernmentEntityId,
                    out string? name)
                    ? name
                    : UNKNOWN_GOVERNMENT_ENTITY_NAME,
                TotalEmployeeCount = employeeCount.TotalEmployeeCount,
                ActiveEmployeeCount = employeeCount.ActiveEmployeeCount
            })
            .OrderBy(entity => entity.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<PagedResponse<PayrollRunSummaryResponse>> SearchAsync(
        PayrollRunFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        PayrollRunFilterCriteria criteria = new()
        {
            GovernmentEntityId = filter.GovernmentEntityId,
            Year = filter.Year,
            IncludeCancelled = filter.IncludeCancelled,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        PagedList<PayrollRun> payrollRuns = await payrollRunRepository.SearchAsync(
            criteria,
            cancellationToken);

        logger.LogInformation(
            "Consulta del historial de nomina. Entidad: {GovernmentEntityId}. " +
            "Ano: {Year}. Resultados: {TotalCount}.",
            filter.GovernmentEntityId,
            filter.Year,
            payrollRuns.TotalCount);

        return PagedResponse<PayrollRunSummaryResponse>.FromPagedList(
            payrollRuns,
            payrollRun => payrollRun.ToSummaryResponse());
    }

    /// <inheritdoc />
    public async Task<PayrollRunDetailResponse> GetByIdAsync(
        Guid payrollRunId,
        CancellationToken cancellationToken = default)
    {
        PayrollRun payrollRun = await payrollRunRepository.GetWithDetailAsync(
            payrollRunId,
            cancellationToken)
            ?? throw new EntityNotFoundException(PAYROLL_RUN_ENTITY_NAME, payrollRunId);

        return payrollRun.ToDetailResponse();
    }

    /// <inheritdoc />
    public async Task<PayrollRunDetailResponse> CancelAsync(
        Guid payrollRunId,
        CancelPayrollRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        PayrollRun payrollRun = await payrollRunRepository.GetWithDetailAsync(
            payrollRunId,
            cancellationToken)
            ?? throw new EntityNotFoundException(PAYROLL_RUN_ENTITY_NAME, payrollRunId);

        if (payrollRun.Status == PayrollRunStatus.Cancelled)
        {
            throw new BusinessRuleViolationException(
                "La ejecucion de nomina ya se encuentra anulada.");
        }

        payrollRun.Status = PayrollRunStatus.Cancelled;
        payrollRun.CancellationReason = request.Reason.Trim();
        payrollRun.CancelledAt = dateTimeProvider.UtcNow;

        await payrollRunRepository.UpdateAsync(payrollRun, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Nomina {PayrollRunId} de la semana {Year}-S{WeekNumber} anulada. Motivo: {Reason}.",
            payrollRun.Id,
            payrollRun.Year,
            payrollRun.WeekNumber,
            payrollRun.CancellationReason);

        return payrollRun.ToDetailResponse();
    }

    /// <inheritdoc />
    public async Task<GeneratedWeeksResponse> GetGeneratedWeeksAsync(
        Guid governmentEntityId,
        int year,
        CancellationToken cancellationToken = default)
    {
        await GetRequiredGovernmentEntityAsync(governmentEntityId, cancellationToken);

        IReadOnlyCollection<int> generatedWeekNumbers =
            await payrollRunRepository.GetGeneratedWeekNumbersAsync(
                governmentEntityId,
                year,
                cancellationToken);

        return new GeneratedWeeksResponse
        {
            GovernmentEntityId = governmentEntityId,
            Year = year,
            WeeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(year),
            GeneratedWeekNumbers = generatedWeekNumbers
        };
    }

    private async Task<IReadOnlyCollection<PayrollRunLine>> BuildLinesAsync(
        Guid governmentEntityId,
        PayrollWeek payrollWeek,
        bool onlyActiveEmployees,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Employee> employees = await employeeRepository.GetForPayrollAsync(
            governmentEntityId,
            onlyActiveEmployees,
            cancellationToken);

        return payrollCalculator.BuildLines(employees, payrollWeek);
    }

    /// <summary>
    /// Obtiene la entidad gubernamental del catalogo, o falla si no existe.
    /// </summary>
    /// <remarks>
    /// La entidad se lee del archivo de texto plano y no de la base de datos
    /// relacional. Es tambien la comprobacion que sustituye a la clave foranea que
    /// no puede existir entre los dos almacenes.
    /// </remarks>
    /// <param name="governmentEntityId">Entidad solicitada.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La entidad gubernamental.</returns>
    private async Task<GovernmentEntity> GetRequiredGovernmentEntityAsync(
        Guid governmentEntityId,
        CancellationToken cancellationToken) =>
        await governmentEntityRepository.GetByIdAsync(governmentEntityId, cancellationToken)
            ?? throw new EntityNotFoundException(GOVERNMENT_ENTITY_NAME, governmentEntityId);
}
