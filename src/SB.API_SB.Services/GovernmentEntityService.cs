using Microsoft.Extensions.Logging;
using SB.API_SB.Application.Common;
using SB.API_SB.Application.Contracts.GovernmentEntities;
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
/// Implementacion de la consulta del listado de entidades gubernamentales de la
/// Republica Dominicana.
/// </summary>
/// <remarks>
/// El servicio no sabe que los datos viven en un archivo de texto plano: solo
/// conoce <see cref="IGovernmentEntityRepository"/>. El listado es un catalogo de
/// solo lectura, distribuido con la aplicacion, y es tambien la fuente que
/// alimenta la asignacion de empleados a entidades.
/// </remarks>
public sealed class GovernmentEntityService : IGovernmentEntityService
{
    private const string ENTITY_NAME = "la entidad gubernamental";

    private readonly IGovernmentEntityRepository entityRepository;
    private readonly ILogger<GovernmentEntityService> logger;

    public GovernmentEntityService(
        IGovernmentEntityRepository entityRepository,
        ILogger<GovernmentEntityService> logger)
    {
        this.entityRepository = entityRepository;
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
        GovernmentEntity entity =
            await entityRepository.GetByIdAsync(entityId, cancellationToken)
            ?? throw new EntityNotFoundException(ENTITY_NAME, entityId);

        return entity.ToResponse();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<GovernmentEntityOptionResponse>>
        GetSelectionOptionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<GovernmentEntity> entities =
            await entityRepository.GetAllAsync(cancellationToken);

        return entities
            .Where(entity => entity.Status == RecordStatus.Active)
            .OrderBy(entity => entity.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(entity => entity.ToOptionResponse())
            .ToList();
    }

    /// <inheritdoc />
    public async Task<GovernmentEntityCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        GovernmentEntityCatalogs catalogs = await entityRepository.GetCatalogsAsync(
            cancellationToken);

        return catalogs.ToResponse();
    }
}
