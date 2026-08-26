namespace SB.API_SB.Domain.Enums;

/// <summary>Estado generico de un registro de mantenimiento.</summary>
public enum RecordStatus
{
    /// <summary>Registro vigente.</summary>
    Active = 1,

    /// <summary>Registro dado de baja logicamente.</summary>
    Inactive = 2
}
