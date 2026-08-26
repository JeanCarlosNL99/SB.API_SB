using SB.API_SB.Infrastructure.Security;
using Xunit;

namespace SB.API_SB.Tests.Infrastructure;

/// <summary>Pruebas de la derivacion y verificacion de contrasenas.</summary>
public sealed class PasswordHasherTests
{
    private const string VALID_PASSWORD = "Sb2026Segura";

    private readonly PasswordHasher passwordHasher = new();

    [Fact]
    public void HashPassword_ContrasenaValida_DevuelveHashDistintoDeLaContrasena()
    {
        (string hash, string salt) = passwordHasher.HashPassword(VALID_PASSWORD);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.False(string.IsNullOrWhiteSpace(salt));
        Assert.NotEqual(VALID_PASSWORD, hash);
    }

    [Fact]
    public void HashPassword_MismaContrasenaDosVeces_GeneraSalesYHashesDistintos()
    {
        (string firstHash, string firstSalt) = passwordHasher.HashPassword(VALID_PASSWORD);
        (string secondHash, string secondSalt) = passwordHasher.HashPassword(VALID_PASSWORD);

        // Cada contrasena usa su propia sal aleatoria: dos usuarios con la misma
        // contrasena no comparten hash, lo que impide identificarlos comparandolos.
        Assert.NotEqual(firstSalt, secondSalt);
        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void VerifyPassword_ContrasenaCorrecta_DevuelveVerdadero()
    {
        (string hash, string salt) = passwordHasher.HashPassword(VALID_PASSWORD);

        Assert.True(passwordHasher.VerifyPassword(VALID_PASSWORD, hash, salt));
    }

    [Theory]
    [InlineData("sb2026segura")]
    [InlineData("Sb2026Segur")]
    [InlineData("otraContrasena1")]
    [InlineData("")]
    public void VerifyPassword_ContrasenaIncorrecta_DevuelveFalso(string attemptedPassword)
    {
        (string hash, string salt) = passwordHasher.HashPassword(VALID_PASSWORD);

        Assert.False(passwordHasher.VerifyPassword(attemptedPassword, hash, salt));
    }

    [Fact]
    public void VerifyPassword_HashOSalCorruptos_DevuelveFalsoSinLanzarExcepcion()
    {
        Assert.False(passwordHasher.VerifyPassword(VALID_PASSWORD, "no-es-base64!", "tampoco!"));
        Assert.False(passwordHasher.VerifyPassword(VALID_PASSWORD, string.Empty, string.Empty));
    }

    [Fact]
    public void HashPassword_ContrasenaVacia_LanzaExcepcionDeArgumento()
    {
        Assert.Throws<ArgumentException>(() => passwordHasher.HashPassword(string.Empty));
        Assert.Throws<ArgumentException>(() => passwordHasher.HashPassword("   "));
    }
}
