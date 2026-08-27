using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Application.Interfaces.Employees;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Application.Mappings;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Services;

/// <summary>
/// Implementacion de los casos de uso del mantenimiento de empleados.
/// </summary>
/// <remarks>
/// El servicio orquesta: valida reglas que requieren consultar la base de datos,
/// delega la construccion de cada subtipo al manejador correspondiente, deja que
/// el dominio calcule el pago y confirma la transaccion con la unidad de trabajo.
/// No contiene ninguna formula de nomina ni ninguna sentencia SQL.
/// </remarks>
public sealed class EmployeeService : IEmployeeService
{
    private const string EMPLOYEE_ENTITY_NAME = "el empleado";
    private const string SOCIAL_SECURITY_NUMBER_FIELD_NAME = "numero de seguro social";

    private readonly IEmployeeRepository employeeRepository;
    private readonly IDepartmentRepository departmentRepository;
    private readonly ICompanyRepository companyRepository;
    private readonly IEmployeeTypeHandlerResolver typeHandlerResolver;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<EmployeeService> logger;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ICompanyRepository companyRepository,
        IEmployeeTypeHandlerResolver typeHandlerResolver,
        IUnitOfWork unitOfWork,
        ILogger<EmployeeService> logger)
    {
        this.employeeRepository = employeeRepository;
        this.departmentRepository = departmentRepository;
        this.companyRepository = companyRepository;
        this.typeHandlerResolver = typeHandlerResolver;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResponse<EmployeeResponse>> SearchAsync(
        EmployeeFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        EmployeeFilterCriteria criteria = new()
        {
            Name = filter.Name,
            CompanyId = filter.CompanyId,
            DepartmentId = filter.DepartmentId,
            Status = filter.Status,
            Type = filter.Type,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        PagedList<Employee> employees = await employeeRepository.SearchAsync(
            criteria,
            cancellationToken);

        logger.LogInformation(
            "Consulta de empleados. Nombre: {Name}. Departamento: {DepartmentId}. " +
            "Estado: {Status}. Resultados: {TotalCount}.",
            filter.Name,
            filter.DepartmentId,
            filter.Status,
            employees.TotalCount);

        // En el listado no se incluye el desglose del calculo: la pantalla solo
        // necesita el monto y el detalle se consulta al abrir un empleado.
        return PagedResponse<EmployeeResponse>.FromPagedList(
            employees,
            employee => MapToResponse(employee, includePaymentBreakdown: false));
    }

    /// <inheritdoc />
    public async Task<EmployeeResponse> GetByIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        Employee employee = await GetRequiredEmployeeAsync(employeeId, cancellationToken);

        return MapToResponse(employee, includePaymentBreakdown: true);
    }

    /// <inheritdoc />
    public async Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);
        await EnsureDepartmentExistsAsync(request.DepartmentId, cancellationToken);
        await EnsureSocialSecurityNumberIsAvailableAsync(
            request.SocialSecurityNumber,
            excludedEmployeeId: null,
            cancellationToken);

        IEmployeeTypeHandler typeHandler = typeHandlerResolver.Resolve(request.Type);

        Employee employee = typeHandler.CreateEmployee(request);
        employee.ApplyCommonValues(request);

        await employeeRepository.AddAsync(employee, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Empleado {EmployeeId} creado. Tipo: {EmployeeType}. Pago semanal: {WeeklyPayment}.",
            employee.Id,
            employee.Type,
            employee.CalculateWeeklyPayment());

        return await GetByIdAsync(employee.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmployeeResponse> UpdateAsync(
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Employee employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EntityNotFoundException(EMPLOYEE_ENTITY_NAME, employeeId);

        if (employee.Type != request.Type)
        {
            throw new BusinessRuleViolationException(
                "No se puede cambiar el tipo de contrato de un empleado ya registrado.");
        }

        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);
        await EnsureDepartmentExistsAsync(request.DepartmentId, cancellationToken);
        await EnsureSocialSecurityNumberIsAvailableAsync(
            request.SocialSecurityNumber,
            employeeId,
            cancellationToken);

        IEmployeeTypeHandler typeHandler = typeHandlerResolver.Resolve(request.Type);

        employee.ApplyCommonValues(request);
        typeHandler.ApplyTypeSpecificValues(employee, request);

        await employeeRepository.UpdateAsync(employee, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Empleado {EmployeeId} actualizado. Pago semanal recalculado: {WeeklyPayment}.",
            employee.Id,
            employee.CalculateWeeklyPayment());

        return await GetByIdAsync(employee.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        Employee employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EntityNotFoundException(EMPLOYEE_ENTITY_NAME, employeeId);

        await employeeRepository.DeleteAsync(employee, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Empleado {EmployeeId} eliminado.", employeeId);
    }

    private async Task<Employee> GetRequiredEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        await employeeRepository.GetWithDepartmentAsync(employeeId, cancellationToken)
            ?? throw new EntityNotFoundException(EMPLOYEE_ENTITY_NAME, employeeId);

    private async Task EnsureCompanyExistsAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        Company? company = await companyRepository.GetByIdAsync(companyId, cancellationToken);

        if (company is null)
        {
            throw new EntityNotFoundException("la compania", companyId);
        }

        if (!company.IsActive)
        {
            throw new BusinessRuleViolationException(
                $"La compania '{company.Name}' esta inactiva y no admite nuevos empleados.");
        }
    }

    private async Task EnsureDepartmentExistsAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        Department? department = await departmentRepository.GetByIdAsync(
            departmentId,
            cancellationToken);

        if (department is null)
        {
            throw new EntityNotFoundException("el departamento", departmentId);
        }

        if (!department.IsActive)
        {
            throw new BusinessRuleViolationException(
                $"El departamento '{department.Name}' esta inactivo y no admite empleados.");
        }
    }

    private async Task EnsureSocialSecurityNumberIsAvailableAsync(
        string socialSecurityNumber,
        Guid? excludedEmployeeId,
        CancellationToken cancellationToken)
    {
        string normalizedNumber = socialSecurityNumber.Trim();

        bool numberAlreadyExists = await employeeRepository.ExistsBySocialSecurityNumberAsync(
            normalizedNumber,
            excludedEmployeeId,
            cancellationToken);

        if (numberAlreadyExists)
        {
            throw new DuplicatedEntityException(
                EMPLOYEE_ENTITY_NAME,
                SOCIAL_SECURITY_NUMBER_FIELD_NAME,
                normalizedNumber);
        }
    }

    private EmployeeResponse MapToResponse(Employee employee, bool includePaymentBreakdown)
    {
        IEmployeeTypeHandler typeHandler = typeHandlerResolver.Resolve(employee.Type);

        return employee.ToResponse(typeHandler, includePaymentBreakdown);
    }
}
