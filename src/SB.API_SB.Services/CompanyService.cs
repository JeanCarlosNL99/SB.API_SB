using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Contracts.Companies;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Application.Mappings;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Services;

/// <summary>Implementacion del mantenimiento de companias.</summary>
public sealed class CompanyService : ICompanyService
{
    private const string COMPANY_ENTITY_NAME = "la compania";
    private const string TAX_IDENTIFICATION_FIELD_NAME = "Registro Nacional de Contribuyente";

    private readonly ICompanyRepository companyRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<CompanyService> logger;

    public CompanyService(
        ICompanyRepository companyRepository,
        IUnitOfWork unitOfWork,
        ILogger<CompanyService> logger)
    {
        this.companyRepository = companyRepository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<CompanyResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Company> companies = await companyRepository.GetAllAsync(
            cancellationToken);

        // El conteo de empleados se resuelve en una sola consulta agregada, no una
        // por compania.
        IReadOnlyDictionary<Guid, int> activeEmployeeCounts =
            await companyRepository.GetActiveEmployeeCountsAsync(cancellationToken);

        return companies
            .Select(company => company.ToResponse(
                activeEmployeeCounts.TryGetValue(company.Id, out int employeeCount)
                    ? employeeCount
                    : 0))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<CompanyResponse> GetByIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        Company company = await GetRequiredCompanyAsync(companyId, cancellationToken);

        IReadOnlyDictionary<Guid, int> activeEmployeeCounts =
            await companyRepository.GetActiveEmployeeCountsAsync(cancellationToken);

        return company.ToResponse(
            activeEmployeeCounts.TryGetValue(company.Id, out int employeeCount)
                ? employeeCount
                : 0);
    }

    /// <inheritdoc />
    public async Task<CompanyResponse> CreateAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string normalizedTaxIdentification = request.TaxIdentificationNumber.Trim();

        await EnsureTaxIdentificationIsAvailableAsync(
            normalizedTaxIdentification,
            excludedCompanyId: null,
            cancellationToken);

        Company company = new()
        {
            Name = request.Name.Trim(),
            TaxIdentificationNumber = normalizedTaxIdentification,
            IsActive = true
        };

        await companyRepository.AddAsync(company, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Compania {CompanyId} creada. Razon social: {CompanyName}.",
            company.Id,
            company.Name);

        return company.ToResponse(activeEmployeeCount: 0);
    }

    /// <inheritdoc />
    public async Task<CompanyResponse> UpdateAsync(
        Guid companyId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Company company = await GetRequiredCompanyAsync(companyId, cancellationToken);

        string normalizedTaxIdentification = request.TaxIdentificationNumber.Trim();

        await EnsureTaxIdentificationIsAvailableAsync(
            normalizedTaxIdentification,
            companyId,
            cancellationToken);

        company.Name = request.Name.Trim();
        company.TaxIdentificationNumber = normalizedTaxIdentification;
        company.IsActive = request.IsActive;

        await companyRepository.UpdateAsync(company, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Compania {CompanyId} actualizada.", company.Id);

        return await GetByIdAsync(company.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        Company company = await GetRequiredCompanyAsync(companyId, cancellationToken);

        if (await companyRepository.HasEmployeesAsync(companyId, cancellationToken))
        {
            throw new BusinessRuleViolationException(
                $"La compania '{company.Name}' tiene empleados registrados y no puede eliminarse. " +
                "Marquela como inactiva o reasigne sus empleados.");
        }

        if (await companyRepository.HasPayrollRunsAsync(companyId, cancellationToken))
        {
            throw new BusinessRuleViolationException(
                $"La compania '{company.Name}' tiene nominas en el historial y no puede " +
                "eliminarse: el historial de pagos debe conservarse.");
        }

        await companyRepository.DeleteAsync(company, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Compania {CompanyId} eliminada.", companyId);
    }

    private async Task<Company> GetRequiredCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken) =>
        await companyRepository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new EntityNotFoundException(COMPANY_ENTITY_NAME, companyId);

    private async Task EnsureTaxIdentificationIsAvailableAsync(
        string taxIdentificationNumber,
        Guid? excludedCompanyId,
        CancellationToken cancellationToken)
    {
        Company? existingCompany = await companyRepository.GetByTaxIdentificationNumberAsync(
            taxIdentificationNumber,
            cancellationToken);

        if (existingCompany is null)
        {
            return;
        }

        if (excludedCompanyId.HasValue && existingCompany.Id == excludedCompanyId.Value)
        {
            return;
        }

        throw new DuplicatedEntityException(
            COMPANY_ENTITY_NAME,
            TAX_IDENTIFICATION_FIELD_NAME,
            taxIdentificationNumber);
    }
}
