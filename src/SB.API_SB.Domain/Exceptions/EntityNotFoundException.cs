namespace SB.API_SB.Domain.Exceptions;

/// <summary>Se lanza cuando el registro solicitado no existe.</summary>
public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object entityIdentifier)
        : base($"No se encontro {entityName} con identificador '{entityIdentifier}'.")
    {
        EntityName = entityName;
        EntityIdentifier = entityIdentifier;
    }

    /// <summary>Nombre de la entidad buscada.</summary>
    public string EntityName { get; }

    /// <summary>Identificador utilizado en la busqueda.</summary>
    public object EntityIdentifier { get; }

    /// <inheritdoc />
    public override string ErrorCode => "ENTIDAD_NO_ENCONTRADA";
}
