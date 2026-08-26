namespace SB.API_SB.Domain.Exceptions;

/// <summary>Se lanza al intentar crear un registro que viola una restriccion de unicidad.</summary>
public sealed class DuplicatedEntityException : DomainException
{
    public DuplicatedEntityException(string entityName, string fieldName, object fieldValue)
        : base($"Ya existe {entityName} con {fieldName} '{fieldValue}'.")
    {
        EntityName = entityName;
        FieldName = fieldName;
        FieldValue = fieldValue;
    }

    /// <summary>Nombre de la entidad duplicada.</summary>
    public string EntityName { get; }

    /// <summary>Campo que provoca el conflicto.</summary>
    public string FieldName { get; }

    /// <summary>Valor duplicado.</summary>
    public object FieldValue { get; }

    /// <inheritdoc />
    public override string ErrorCode => "REGISTRO_DUPLICADO";
}
