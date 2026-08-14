using System;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity.Tests;

/// <summary>
/// Tests for <see cref="IdentityTokenRequest"/> — what a caller is asking for, and how that keys the
/// cache.
/// </summary>
public class IdentityTokenRequestTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorRejectsAMissingAudience()
    {
        // NO FALLBACKS: a token whose audience nobody declared has a blast radius nobody declared.
        Should.Throw<ArgumentException>(() => new IdentityTokenRequest(null!));
        Should.Throw<ArgumentException>(() => new IdentityTokenRequest(""));
        Should.Throw<ArgumentException>(() => new IdentityTokenRequest("   "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CacheKeyIsStableAcrossScopeOrdering()
    {
        // Two callers asking for the same set in a different sequence must share one cache entry
        // rather than each acquiring its own token.
        new IdentityTokenRequest("https://etl.example.dev", ["read", "write"]).CacheKey
            .ShouldBe(new IdentityTokenRequest("https://etl.example.dev", ["write", "read"]).CacheKey);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CacheKeyDistinguishesAudiences()
    {
        new IdentityTokenRequest("https://etl.example.dev").CacheKey
            .ShouldNotBe(new IdentityTokenRequest("https://api.example.dev").CacheKey);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CacheKeyDistinguishesScopeSets()
    {
        // A narrower token must not be served to a caller that asked for more.
        new IdentityTokenRequest("https://etl.example.dev", ["read"]).CacheKey
            .ShouldNotBe(new IdentityTokenRequest("https://etl.example.dev", ["read", "write"]).CacheKey);
    }
}
