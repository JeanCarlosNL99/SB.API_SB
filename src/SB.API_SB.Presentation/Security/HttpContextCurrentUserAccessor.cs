using System.Security.Claims;
using SB.API_SB.Application.Interfaces.Security;

namespace SB.API_SB.Presentation.Security;

/// <summary>
/// Obtiene la identidad del usuario autenticado desde el contexto HTTP.
/// </summary>
/// <remarks>
/// Es el unico punto de la solucion que depende de <c>HttpContext</c> para
/// conocer al usuario. Las capas internas usan la abstraccion
/// <see cref="ICurrentUserAccessor"/>, por lo que pueden probarse sin un
/// servidor web.
/// </remarks>
public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    /// <summary>Nombre utilizado cuando la peticion no esta autenticada.</summary>
    public const string ANONYMOUS_USER_NAME = "Anonimo";

    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string UserName
    {
        get
        {
            ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                return ANONYMOUS_USER_NAME;
            }

            string? userName = principal.FindFirst("userName")?.Value
                ?? principal.Identity.Name;

            return string.IsNullOrWhiteSpace(userName) ? ANONYMOUS_USER_NAME : userName;
        }
    }

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            string? subjectClaimValue = httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(subjectClaimValue, out Guid userId) ? userId : null;
        }
    }
}
