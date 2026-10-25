using System;
using System.Linq;
using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Validation;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// FDW-2363: a token from another issuer carries none of the platform's authority claims, so only what this platform states reaches the principal.
/// </summary>
public sealed class ForeignClaimsTests
{
    private static ClaimsIdentity Identity(params (string Type, string Value)[] claims)
        => new(claims.Select(claim => new Claim(claim.Type, claim.Value)), "foreign");

    [Fact]
    public void EveryPlatformAuthorityClaimIsRemoved()
    {
        var authority = ClaimDefinitions.All().Select(definition => definition.Name)
            .Where(name => name != ClaimDefinitions.sub.Name && name != ClaimDefinitions.jti.Name)
            .ToList();
        var identity = Identity([("sub", "person"), .. authority.Select(name => (name, "forged"))]);

        var removed = ForeignClaims.WithoutPlatformClaims(identity);

        removed.OrderBy(type => type, StringComparer.Ordinal).ToArray().ShouldBe(authority.OrderBy(type => type, StringComparer.Ordinal).ToArray());
        identity.Claims.Select(claim => claim.Type).ToArray().ShouldBe(new[] { "sub" });
    }

    [Fact]
    public void ThePlatformClaimsThatMatterAreAmongThoseRemoved()
    {
        var identity = Identity(
            (ClaimDefinitions.perm.Name, "roles:write"),
            (ClaimDefinitions.roles.Name, "Admin"),
            (ClaimDefinitions.tenantId.Name, "aaaaaaaa-0000-0000-0000-000000000001"),
            (ClaimDefinitions.actingRole.Name, "Admin"),
            (ClaimDefinitions.actor.Name, "someone"));

        ForeignClaims.WithoutPlatformClaims(identity).Count.ShouldBe(5);

        identity.Claims.ShouldBeEmpty();
    }

    [Fact]
    public void TheIssuersOwnSubjectAndTokenIdStay()
    {
        var identity = Identity((ClaimDefinitions.sub.Name, "person"), (ClaimDefinitions.jti.Name, "token-1"));

        ForeignClaims.WithoutPlatformClaims(identity).ShouldBeEmpty();

        identity.FindFirst(ClaimDefinitions.sub.Name)?.Value.ShouldBe("person");
        identity.FindFirst(ClaimDefinitions.jti.Name)?.Value.ShouldBe("token-1");
    }

    [Fact]
    public void ClaimsThatAreNotThePlatformsStay()
    {
        var identity = Identity(("name", "Jo"), ("email", "jo@example.test"), ("iss", "https://issuer.test"), (ClaimDefinitions.perm.Name, "forged"));

        var removed = ForeignClaims.WithoutPlatformClaims(identity);

        removed.ShouldBe(new[] { ClaimDefinitions.perm.Name });
        identity.Claims.Select(claim => claim.Type).ToArray().ShouldBe(new[] { "name", "email", "iss" });
    }

    [Fact]
    public void EveryValueOfARepeatedPlatformClaimIsRemoved()
    {
        var identity = Identity((ClaimDefinitions.perm.Name, "a"), (ClaimDefinitions.perm.Name, "b"), (ClaimDefinitions.perm.Name, "c"));

        ForeignClaims.WithoutPlatformClaims(identity).Count.ShouldBe(3);

        identity.Claims.ShouldBeEmpty();
    }
}
