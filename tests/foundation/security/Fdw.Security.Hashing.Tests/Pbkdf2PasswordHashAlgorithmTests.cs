using System;
using Fdw.Security.Hashing;
using Shouldly;

namespace Fdw.Security.Hashing.Tests;

public sealed class Pbkdf2PasswordHashAlgorithmTests
{
    private readonly Pbkdf2PasswordHashAlgorithm _algorithm = new();

    [Fact]
    public void AHashVerifiesWithTheSamePassword()
    {
        var hashed = _algorithm.HashPassword("correct horse");

        hashed.AlgorithmName.ShouldBe("Pbkdf2");
        Convert.FromBase64String(hashed.Salt).Length.ShouldBe(16);
        Convert.FromBase64String(hashed.Hash).Length.ShouldBe(32);
        _algorithm.VerifyPassword("correct horse", hashed.Hash, hashed.Salt).ShouldBeTrue();
    }

    [Fact]
    public void AWrongPasswordDoesNotVerify()
    {
        var hashed = _algorithm.HashPassword("correct horse");

        _algorithm.VerifyPassword("correct horsf", hashed.Hash, hashed.Salt).ShouldBeFalse();
    }

    [Fact]
    public void TheSamePasswordHashesWithADifferentSaltEachTime()
    {
        var first = _algorithm.HashPassword("same");
        var second = _algorithm.HashPassword("same");

        first.Salt.ShouldNotBe(second.Salt);
        first.Hash.ShouldNotBe(second.Hash);
    }

    [Fact]
    public void DeriveKeyReproducesTheStoredHash()
    {
        var hashed = _algorithm.HashPassword("edge kdf");

        Convert.ToBase64String(_algorithm.DeriveKey("edge kdf", hashed.Salt)).ShouldBe(hashed.Hash);
    }

    [Theory]
    [InlineData("not base64 !!", "AAAAAAAAAAAAAAAAAAAAAA==")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", "not base64 !!")]
    public void AStoredValueThatIsNotBase64DoesNotVerify(string storedHash, string storedSalt)
        => _algorithm.VerifyPassword("anything", storedHash, storedSalt).ShouldBeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("AAAA")]
    public void AnEmptyOrShortStoredHashDoesNotVerify(string storedHash)
    {
        var hashed = _algorithm.HashPassword("anything");

        _algorithm.VerifyPassword("anything", storedHash, hashed.Salt).ShouldBeFalse();
    }
}
