using SB.API_SB.Application.Contracts.GovernmentEntities;
using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Interfaces.Repositories;

namespace SB.API_SB.Application.Mappings;

/// <summary>Proyecciones del mantenimiento de entidades gubernamentales.</summary>
public static class GovernmentEntityMappings
{
    /// <summary>Convierte la entidad de dominio en su respuesta de API.</summary>
    /// <param name="entity">Entidad gubernamental.</param>
    /// <returns>Respuesta lista para devolverse desde la API.</returns>
    public static GovernmentEntityResponse ToResponse(this GovernmentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new GovernmentEntityResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Category = entity.Category,
            StateBranch = entity.StateBranch,
            Sector = entity.Sector,
            Status = entity.Status,
            StatusDescription = entity.Status.Describe(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>Reduce la entidad a las dos propiedades que usa un selector.</summary>
    /// <param name="entity">Entidad gubernamental.</param>
    /// <returns>Opcion lista para alimentar un selector de la interfaz.</returns>
    public static GovernmentEntityOptionResponse ToOptionResponse(this GovernmentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new GovernmentEntityOptionResponse
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }

    /// <summary>Convierte los catalogos de dominio en su respuesta de API.</summary>
    /// <param name="catalogs">Catalogos obtenidos del repositorio.</param>
    /// <returns>Catalogos listos para alimentar los filtros de la interfaz.</returns>
    public static GovernmentEntityCatalogsResponse ToResponse(
        this GovernmentEntityCatalogs catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);

        return new GovernmentEntityCatalogsResponse
        {
            Categories = catalogs.Categories,
            Sectors = catalogs.Sectors,
            StateBranches = catalogs.StateBranches
        };
    }
}
