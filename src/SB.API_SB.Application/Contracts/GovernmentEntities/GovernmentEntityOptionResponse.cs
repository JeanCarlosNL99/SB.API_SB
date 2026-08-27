namespace SB.API_SB.Application.Contracts.GovernmentEntities;

/// <summary>
/// Entidad gubernamental reducida a lo que necesita un selector: identificador y
/// nombre.
/// </summary>
/// <remarks>
/// Existe como contrato aparte porque el selector de entidades del formulario de
/// empleados y del calculo de nomina necesita el listado completo, no una pagina.
/// Resolverlo pidiendo una pagina muy grande funcionaria hoy, con 181 entidades,
/// y empezaria a recortar el listado en silencio el dia que el listado oficial
/// supere el tamano maximo de pagina. Un contrato propio, sin paginacion y con
/// solo dos campos, evita ese fallo silencioso.
/// </remarks>
public sealed class GovernmentEntityOptionResponse
{
    /// <summary>Identificador de la entidad gubernamental.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre oficial de la entidad gubernamental.</summary>
    public string Name { get; set; } = string.Empty;
}
