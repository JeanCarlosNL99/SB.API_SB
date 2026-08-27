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
/// Implementacion del calculo de pagos semanales por compania.
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
    private const string COMPANY_ENTITY_NAME = "la compania";

    private readonly IPayrollRunRepository payrollRunRepository;
    private readonly ICompanyRepository companyRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IPayrollCalculator payrollCalculator;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<PayrollRunService> logger;

    public PayrollRunService(
        IPayrollRunRepository payrollRunRepository,
        ICompanyRepository companyRepository,
        IEmployeeRepository employeeRepository,
        IPayrollCalculator payrollCalculator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<PayrollRunService> logger)
    {
        this.payrollRunRepository = payrollRunRepository;
        this.companyRepository = companyRepository;
        this.employeeRepository = employeeRepository;
        this.payrollCalculator = payrollCalculator;
        this.dateTimeProvider = dateTimeProvider;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PayrollPreviewResponse> PreviewAsync(
        Guid companyId,
        int year,
        int weekNumber,
        bool onlyActiveEmployees,
        CancellationToken cancellationToken = default)
    {
        Company company = await GetRequiredCompanyAsync(companyId, cancellationToken);
        PayrollWeek payrollWeek = PayrollWeek.Create(year, weekNumber);

        PayrollRun? existingRun = await payrollRunRepository.FindGeneratedRunAsync(
            companyId,
            payrollWeek,
            cancellationToken);

        IReadOnlyCollection<PayrollRunLine> lines = await BuildLinesAsync(
            companyId,
            payrollWeek,
            onlyActiveEmployees,
            cancellationToken);

        List<PayrollRunLineResponse> lineResponses = lines
            .Select(line => line.ToResponse())
            .ToList();

        logger.LogInformation(
            "Vista previa de nomina para {CompanyName}, semana {WeekLabel}. " +
            "Empleados: {EmployeeCount}. Total: {TotalAmount}. Ya generada: {IsAlreadyGenerated}.",
            company.Name,
            payrollWeek.Label,
            lineResponses.Count,
            lineResponses.Sum(line => line.WeeklyPayment),
            existingRun is not null);

        return new PayrollPreviewResponse
        {
            CompanyId = company.Id,
            CompanyName = company.Name,
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

        Company company = await GetRequiredCompanyAsync(request.CompanyId, cancellationToken);

        if (!company.IsActive)
        {
            throw new BusinessRuleViolationException(
                $"La compania '{company.Name}' esta inactiva y no admite generacion de nomina.");
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
            request.CompanyId,
            payrollWeek,
            cancellationToken);

        if (existingRun is not null)
        {
            logger.LogWarning(
                "Se rechazo la generacion de nomina de {CompanyName} para la semana " +
                "{WeekLabel}: ya existe la ejecucion {ExistingPayrollRunId}.",
                company.Name,
                payrollWeek.Label,
                existingRun.Id);

            throw new DuplicatedPayrollRunException(
                company.Name,
                payrollWeek.Year,
                payrollWeek.WeekNumber,
                existingRun.Id);
        }

        IReadOnlyCollection<PayrollRunLine> lines = await BuildLinesAsync(
            request.CompanyId,
            payrollWeek,
            request.OnlyActiveEmployees,
            cancellationToken);

        if (lines.Count == 0)
        {
            throw new BusinessRuleViolationException(
                $"La compania '{company.Name}' no tiene empleados que incluir en la nomina de " +
                $"la semana {payrollWeek.Label}.");
        }

        PayrollRun payrollRun = new()
        {
            CompanyId = company.Id,
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
            "Nomina generada para {CompanyName}, semana {WeekLabel}. Ejecucion: {PayrollRunId}. " +
            "Empleados: {EmployeeCount}. Total: {TotalAmount}.",
            company.Name,
            payrollWeek.Label,
            payrollRun.Id,
            payrollRun.EmployeeCount,
            payrollRun.TotalAmount);

        return await GetByIdAsync(payrollRun.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<PayrollRunSummaryResponse>> SearchAsync(
        PayrollRunFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        PayrollRunFilterCriteria criteria = new()
        {
            CompanyId = filter.CompanyId,
            Year = filter.Year,
            IncludeCancelled = filter.IncludeCancelled,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        PagedList<PayrollRun> payrollRuns = await payrollRunRepository.SearchAsync(
            criteria,
            cancellationToken);

        logger.LogInformation(
            "Consulta del historial de nomina. Compania: {CompanyId}. Ano: {Year}. " +
            "Resultados: {TotalCount}.",
            filter.CompanyId,
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
        Guid companyId,
        int year,
        CancellationToken cancellationToken = default)
    {
        await GetRequiredCompanyAsync(companyId, cancellationToken);

        IReadOnlyCollection<int> generatedWeekNumbers =
            await payrollRunRepository.GetGeneratedWeekNumbersAsync(
                companyId,
                year,
                cancellationToken);

        return new GeneratedWeeksResponse
        {
            CompanyId = companyId,
            Year = year,
            WeeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(year),
            GeneratedWeekNumbers = generatedWeekNumbers
        };
    }

    private async Task<IReadOnlyCollection<PayrollRunLine>> BuildLinesAsync(
        Guid companyId,
        PayrollWeek payrollWeek,
        bool onlyActiveEmployees,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Employee> employees = await employeeRepository.GetForPayrollAsync(
            companyId,
            onlyActiveEmployees,
            cancellationToken);

        return payrollCalculator.BuildLines(employees, payrollWeek);
    }

    private async Task<Company> GetRequiredCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken) =>
        await companyRepository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new EntityNotFoundException(COMPANY_ENTITY_NAME, companyId);
}
