using SB.API_SB.Application.Interfaces.Security;

namespace SB.API_SB.Tests.TestDoubles;

/// <summary>Usuario fijo para las pruebas, sin necesidad de contexto HTTP.</summary>
public sealed class StubCurrentUserAccessor : ICurrentUserAccessor
{
    public StubCurrentUserAccessor(string userName = "pruebas", Guid? userId = null)
    {
        UserName = userName;
        UserId = userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    /// <inheritdoc />
    public string UserName { get; }

    /// <inheritdoc />
    public Guid? UserId { get; }
}
