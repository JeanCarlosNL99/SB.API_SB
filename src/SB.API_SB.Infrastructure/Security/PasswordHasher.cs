using System.Security.Cryptography;
using SB.API_SB.Application.Interfaces.Security;

namespace SB.API_SB.Infrastructure.Security;

/// <summary>
/// Derivacion de contrasenas con PBKDF2 (Rfc2898) y SHA-256.
/// </summary>
/// <remarks>
/// Se eligio PBKDF2 porque forma parte de la biblioteca base de .NET, no agrega
/// dependencias externas y es un algoritmo de derivacion lento por diseno: cada
/// contrasena usa una sal aleatoria distinta y un numero elevado de iteraciones,
/// lo que encarece los ataques de diccionario. La comparacion final se hace en
/// tiempo fijo para no filtrar informacion por el tiempo de respuesta.
/// </remarks>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SALT_SIZE_IN_BYTES = 16;
    private const int HASH_SIZE_IN_BYTES = 32;
    private const int ITERATION_COUNT = 100_000;

    private static readonly HashAlgorithmName HASH_ALGORITHM = HashAlgorithmName.SHA256;

    /// <inheritdoc />
    public (string Hash, string Salt) HashPassword(string plainTextPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainTextPassword);

        byte[] saltBytes = RandomNumberGenerator.GetBytes(SALT_SIZE_IN_BYTES);
        byte[] hashBytes = DeriveHash(plainTextPassword, saltBytes);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    /// <inheritdoc />
    public bool VerifyPassword(string plainTextPassword, string storedHash, string storedSalt)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword) ||
            string.IsNullOrWhiteSpace(storedHash) ||
            string.IsNullOrWhiteSpace(storedSalt))
        {
            return false;
        }

        byte[] saltBytes;
        byte[] expectedHashBytes;

        try
        {
            saltBytes = Convert.FromBase64String(storedSalt);
            expectedHashBytes = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            // Un hash o una sal corruptos no deben tumbar la autenticacion:
            // simplemente no coinciden.
            return false;
        }

        byte[] actualHashBytes = DeriveHash(plainTextPassword, saltBytes);

        return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }

    private static byte[] DeriveHash(string plainTextPassword, byte[] saltBytes) =>
        Rfc2898DeriveBytes.Pbkdf2(
            plainTextPassword,
            saltBytes,
            ITERATION_COUNT,
            HASH_ALGORITHM,
            HASH_SIZE_IN_BYTES);
}
