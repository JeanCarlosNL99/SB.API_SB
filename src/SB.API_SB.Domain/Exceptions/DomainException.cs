namespace SB.API_SB.Domain.Exceptions;

/// <summary>
/// Excepcion base de las reglas de negocio. Permite que el middleware de manejo
/// de excepciones distinga un error previsible del dominio de un fallo tecnico
/// inesperado, y traducirlo al codigo HTTP correcto.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Codigo estable del error, util para el cliente y para los logs.</summary>
    public abstract string ErrorCode { get; }
}
