namespace SB.API_SB.Application.Interfaces.Security;

/// <summary>
/// Expone la identidad del usuario que ejecuta la peticion actual. Se usa para
/// completar los campos de auditoria sin acoplar los servicios a ASP.NET Core.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>Nombre del usuario autenticado, o un valor por defecto si no hay sesion.</summary>
    string UserName { get; }

    /// <summary>Identificador del usuario autenticado, si esta disponible.</summary>
    Guid? UserId { get; }
}
