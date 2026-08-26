using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.GovernmentEntities;
using SB.API_SB.Application.Interfaces.Common;
using SB.API_SB.Application.Interfaces.Security;
using SB.API_SB.Application.Interfaces.Services;
using SB.API_SB.Application.Mappings;
using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.Interfaces.Criteria;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Services;

/// <summary>
/// Implementacion del mantenimiento de entidades gubernamentales de la Republica
/// Dominicana.
/// </summary>
/// <remarks>
/// El servicio no sabe que los datos viven en un archivo de texto plano: solo
/// conoce <see cref="IGovernmentEntityRepository"/>. Por eso la auditoria se
/// completa aqui, donde si esta disponible el usuario de la peticion.
/// </remarks>
public sealed class GovernmentEntityService : IGovernmentEntityService
{
    private const string ENTITY_NAME = "la entidad gubernamental";
    private const string NAME_FIELD_NAME = "nombre";

    private readonly IGovernmentEntityRepository entityRepository;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly ILogger<GovernmentEntityService> logger;

    public GovernmentEntityService(
        IGovernmentEntityRepository entityRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<GovernmentEntityService> logger)
    {
        this.entityRepository = entityRepository;
        this.dateTimeProvider = dateTimeProvider;
        this.currentUserAccessor = currentUserAccessor;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResponse<GovernmentEntityResponse>> SearchAsync(
        GovernmentEntityFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        GovernmentEntityFilterCriteria criteria = new()
        {
            Name = filter.Name,
            Category = filter.Category,
            Sector = filter.Sector,
            StateBranch = filter.StateBranch,
            Status = filter.Status,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        PagedList<GovernmentEntity> entities = await entityRepository.SearchAsync(
            criteria,
            cancellationToken);

        logger.LogInformation(
            "Consulta de entidades gubernamentales. Nombre: {Name}. Categoria: {Category}. " +
            "Sector: {Sector}. Resultados: {TotalCount}.",
            filter.Name,
            filter.Category,
            filter.Sector,
            entities.TotalCount);

        return PagedResponse<GovernmentEntityResponse>.FromPagedList(
            entities,
            entity => entity.ToResponse());
    }

    /// <inheritdoc />
    public async Task<GovernmentEntityResponse> GetByIdAsync(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        GovernmentEntity entity = await GetRequiredEntityAsync(entityId, cancellationToken);

        return entity.ToResponse();
    }

    /// <inheritdoc />
    public async Task<GovernmentEntityResponse> CreateAsync(
        CreateGovernmentEntityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string normalizedName = request.Name.Trim();

        await EnsureNameIsAvailableAsync(normalizedName, excludedEntityId: null, cancellationToken);

        GovernmentEntity entity = new()
        {
            Name = normalizedName,
            Category = request.Category.Trim(),
            StateBranch = request.StateBranch.Trim(),
            Sector = request.Sector.Trim(),
            Status = RecordStatus.Active,
            CreatedAt = dateTimeProvider.UtcNow,
            CreatedBy = currentUserAccessor.UserName
        };

        await entityRepository.AddAsync(entity, cancellationToken);

        logger.LogInformation(
            "Entidad gubernamental {EntityId} creada por {UserName}. Nombre: {Name}.",
            entity.Id,
            entity.CreatedBy,
            entity.Name);

        return entity.ToResponse();
    }

    /// <inheritdoc />
    public async Task<GovernmentEntityResponse> UpdateAsync(
        Guid entityId,
        UpdateGovernmentEntityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        GovernmentEntity entity = await GetRequiredEntityAsync(entityId, cancellationToken);

        string normalizedName = request.Name.Trim();

        await EnsureNameIsAvailableAsync(normalizedName, entityId, cancellationToken);

        entity.Name = normalizedName;
        entity.Category = request.Category.Trim();
        entity.StateBranch = request.StateBranch.Trim();
        entity.Sector = request.Sector.Trim();
        entity.Status = request.Status;
        entity.UpdatedAt = dateTimeProvider.UtcNow;
        entity.UpdatedBy = currentUserAccessor.UserName;

        await entityRepository.UpdateAsync(entity, cancellationToken);

        logger.LogInformation(
            "Entidad gubernamental {EntityId} actualizada por {UserName}.",
            entity.Id,
            entity.UpdatedBy);

        return entity.ToResponse();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        GovernmentEntity entity = await GetRequiredEntityAsync(entityId, cancellationToken);

        await entityRepository.DeleteAsync(entity, cancellationToken);

        logger.LogWarning(
            "Entidad gubernamental {EntityId} eliminada por {UserName}.",
            entityId,
            currentUserAccessor.UserName);
    }

    /// <inheritdoc />
    public async Task<GovernmentEntityCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        GovernmentEntityCatalogs catalogs = await entityRepository.GetCatalogsAsync(
            cancellationToken);

        return catalogs.ToResponse();
    }

    private async Task<GovernmentEntity> GetRequiredEntityAsync(
        Guid entityId,
        CancellationToken cancellationToken) =>
        await entityRepository.GetByIdAsync(entityId, cancellationToken)
            ?? throw new EntityNotFoundException(ENTITY_NAME, entityId);

    private async Task EnsureNameIsAvailableAsync(
        string name,
        Guid? excludedEntityId,
        CancellationToken cancellationToken)
    {
        bool nameAlreadyExists = await entityRepository.ExistsByNameAsync(
            name,
            excludedEntityId,
            cancellationToken);

        if (nameAlreadyExists)
        {
            throw new DuplicatedEntityException(ENTITY_NAME, NAME_FIELD_NAME, name);
        }
    }
}
