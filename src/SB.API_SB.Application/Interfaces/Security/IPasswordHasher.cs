namespace SB.API_SB.Application.Interfaces.Security;

/// <summary>Contrato de derivacion y verificacion de contrasenas.</summary>
public interface IPasswordHasher
{
    /// <summary>Genera el hash y la sal de una contrasena en texto claro.</summary>
    /// <param name="plainTextPassword">Contrasena en texto claro.</param>
    /// <returns>Hash y sal codificados en Base64.</returns>
    (string Hash, string Salt) HashPassword(string plainTextPassword);

    /// <summary>Verifica una contrasena contra su hash almacenado.</summary>
    /// <param name="plainTextPassword">Contrasena en texto claro a verificar.</param>
    /// <param name="storedHash">Hash almacenado en Base64.</param>
    /// <param name="storedSalt">Sal almacenada en Base64.</param>
    /// <returns>Verdadero si la contrasena coincide.</returns>
    bool VerifyPassword(string plainTextPassword, string storedHash, string storedSalt);
}
