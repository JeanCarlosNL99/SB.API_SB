using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Contracts.Departments;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Application.Mappings;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Services;

/// <summary>Implementacion del mantenimiento de departamentos.</summary>
public sealed class DepartmentService : IDepartmentService
{
    private const string DEPARTMENT_ENTITY_NAME = "el departamento";
    private const string CODE_FIELD_NAME = "codigo";

    private readonly IDepartmentRepository departmentRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<DepartmentService> logger;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<DepartmentService> logger)
    {
        this.departmentRepository = departmentRepository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DepartmentResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Department> departments = await departmentRepository.GetAllAsync(
            cancellationToken);

        return departments.Select(department => department.ToResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<DepartmentResponse> GetByIdAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        Department department = await GetRequiredDepartmentAsync(departmentId, cancellationToken);

        return department.ToResponse();
    }

    /// <inheritdoc />
    public async Task<DepartmentResponse> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string normalizedCode = request.Code.Trim().ToUpperInvariant();

        await EnsureCodeIsAvailableAsync(normalizedCode, excludedDepartmentId: null, cancellationToken);

        Department department = new()
        {
            Name = request.Name.Trim(),
            Code = normalizedCode,
            IsActive = true
        };

        await departmentRepository.AddAsync(department, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Departamento {DepartmentId} creado. Codigo: {Code}.",
            department.Id,
            department.Code);

        return department.ToResponse();
    }

    /// <inheritdoc />
    public async Task<DepartmentResponse> UpdateAsync(
        Guid departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Department department = await GetRequiredDepartmentAsync(departmentId, cancellationToken);

        string normalizedCode = request.Code.Trim().ToUpperInvariant();

        await EnsureCodeIsAvailableAsync(normalizedCode, departmentId, cancellationToken);

        department.Name = request.Name.Trim();
        department.Code = normalizedCode;
        department.IsActive = request.IsActive;

        await departmentRepository.UpdateAsync(department, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Departamento {DepartmentId} actualizado.", department.Id);

        return department.ToResponse();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        Department department = await GetRequiredDepartmentAsync(departmentId, cancellationToken);

        bool hasEmployees = await departmentRepository.HasEmployeesAsync(
            departmentId,
            cancellationToken);

        if (hasEmployees)
        {
            throw new BusinessRuleViolationException(
                $"El departamento '{department.Name}' tiene empleados asignados y no puede eliminarse. " +
                "Marquelo como inactivo o reasigne los empleados.");
        }

        await departmentRepository.DeleteAsync(department, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Departamento {DepartmentId} eliminado.", departmentId);
    }

    private async Task<Department> GetRequiredDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken) =>
        await departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new EntityNotFoundException(DEPARTMENT_ENTITY_NAME, departmentId);

    private async Task EnsureCodeIsAvailableAsync(
        string code,
        Guid? excludedDepartmentId,
        CancellationToken cancellationToken)
    {
        Department? existingDepartment = await departmentRepository.GetByCodeAsync(
            code,
            cancellationToken);

        if (existingDepartment is null)
        {
            return;
        }

        if (excludedDepartmentId.HasValue && existingDepartment.Id == excludedDepartmentId.Value)
        {
            return;
        }

        throw new DuplicatedEntityException(DEPARTMENT_ENTITY_NAME, CODE_FIELD_NAME, code);
    }
}
