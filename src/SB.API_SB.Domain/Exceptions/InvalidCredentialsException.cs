namespace SB.API_SB.Domain.Exceptions;

/// <summary>
/// Se lanza cuando las credenciales de acceso no son validas. El mensaje es
/// deliberadamente generico para no revelar si el usuario existe.
/// </summary>
public sealed class InvalidCredentialsException : DomainException
{
    private const string GENERIC_MESSAGE = "Las credenciales proporcionadas no son validas.";

    public InvalidCredentialsException()
        : base(GENERIC_MESSAGE)
    {
    }

    /// <inheritdoc />
    public override string ErrorCode => "CREDENCIALES_INVALIDAS";
}
