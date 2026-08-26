namespace SB.API_SB.Application.Contracts.GovernmentEntities;

/// <summary>
/// Catalogos que alimentan los combos de filtro de la interfaz, derivados de los
/// datos realmente almacenados en el mantenimiento.
/// </summary>
public sealed class GovernmentEntityCatalogsResponse
{
    /// <summary>Categorias administrativas registradas.</summary>
    public IReadOnlyCollection<string> Categories { get; set; } = Array.Empty<string>();

    /// <summary>Sectores registrados.</summary>
    public IReadOnlyCollection<string> Sectors { get; set; } = Array.Empty<string>();

    /// <summary>Poderes del Estado registrados.</summary>
    public IReadOnlyCollection<string> StateBranches { get; set; } = Array.Empty<string>();
}
