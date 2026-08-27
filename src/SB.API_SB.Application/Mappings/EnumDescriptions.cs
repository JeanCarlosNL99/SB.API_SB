using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Mappings;

/// <summary>
/// Traduce las enumeraciones del dominio a etiquetas legibles en espanol. Se
/// resuelve en la capa de Aplicacion para que el dominio no cargue con textos de
/// presentacion y para que la interfaz no tenga que duplicar el diccionario.
/// </summary>
public static class EnumDescriptions
{
    private static readonly Dictionary<EmployeeStatus, string> EMPLOYEE_STATUS_DESCRIPTIONS =
        new()
        {
            [EmployeeStatus.Active] = "Activo",
            [EmployeeStatus.Inactive] = "Inactivo"
        };

    private static readonly Dictionary<PayrollRunStatus, string> PAYROLL_RUN_STATUS_DESCRIPTIONS =
        new()
        {
            [PayrollRunStatus.Generated] = "Generada",
            [PayrollRunStatus.Cancelled] = "Anulada"
        };

    private static readonly Dictionary<RecordStatus, string> RECORD_STATUS_DESCRIPTIONS =
        new()
        {
            [RecordStatus.Active] = "Activo",
            [RecordStatus.Inactive] = "Inactivo"
        };

    /// <summary>Obtiene la descripcion del estado laboral de un empleado.</summary>
    /// <param name="status">Estado a describir.</param>
    /// <returns>Etiqueta legible del estado.</returns>
    public static string Describe(this EmployeeStatus status) =>
        EMPLOYEE_STATUS_DESCRIPTIONS.TryGetValue(status, out string? description)
            ? description
            : status.ToString();

    /// <summary>Obtiene la descripcion del estado de una ejecucion de nomina.</summary>
    /// <param name="status">Estado a describir.</param>
    /// <returns>Etiqueta legible del estado.</returns>
    public static string Describe(this PayrollRunStatus status) =>
        PAYROLL_RUN_STATUS_DESCRIPTIONS.TryGetValue(status, out string? description)
            ? description
            : status.ToString();

    /// <summary>Obtiene la descripcion del estado de un registro de mantenimiento.</summary>
    /// <param name="status">Estado a describir.</param>
    /// <returns>Etiqueta legible del estado.</returns>
    public static string Describe(this RecordStatus status) =>
        RECORD_STATUS_DESCRIPTIONS.TryGetValue(status, out string? description)
            ? description
            : status.ToString();
}
