using System;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity.Tests;

/// <summary>
/// Tests for <see cref="IssuedIdentityToken"/> — what a caller needs to decide whether a token is
/// still usable.
/// </summary>
public class IssuedIdentityTokenTests
{
    private static IssuedIdentityToken Token(TimeSpan validFor)
        => new("v", "Bearer", "https://login.example.dev", "https://etl.example.dev", DateTimeOffset.UtcNow + validFor);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorRejectsAnIncompleteToken()
    {
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5);
        Should.Throw<ArgumentException>(() => new IssuedIdentityToken("", "Bearer", "iss", "aud", expiry));
        Should.Throw<ArgumentException>(() => new IssuedIdentityToken("v", "", "iss", "aud", expiry));
        Should.Throw<ArgumentException>(() => new IssuedIdentityToken("v", "Bearer", "", "aud", expiry));
        Should.Throw<ArgumentException>(() => new IssuedIdentityToken("v", "Bearer", "iss", "", expiry));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsUsableAtTreatsATokenInsideTheSkewAsUnusable()
    {
        Token(TimeSpan.FromSeconds(30)).IsUsableAt(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(60)).ShouldBeFalse();
        Token(TimeSpan.FromMinutes(10)).IsUsableAt(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(60)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsUsableAtTreatsAnExpiredTokenAsUnusable()
        => Token(TimeSpan.FromMinutes(-1)).IsUsableAt(DateTimeOffset.UtcNow, TimeSpan.Zero).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AuthorizationHeaderValueUsesTheProvidersOwnTokenType()
    {
        new IssuedIdentityToken("abc", "DPoP", "iss", "aud", DateTimeOffset.UtcNow.AddMinutes(5))
            .AuthorizationHeaderValue.ShouldBe("DPoP abc");
    }
}
