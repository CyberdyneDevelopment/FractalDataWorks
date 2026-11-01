using System;
using System.Linq;
using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Validation;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// FDW-2363, FDW-2933: a token from another issuer keeps the platform claims its entry accepts and none of the others, so only what the deployer
/// has chosen to trust reaches the principal.
/// </summary>
public sealed class ForeignClaimsTests
{
    private static readonly string[] AcceptsNothing = [];

    private static ClaimsIdentity Identity(params (string Type, string Value)[] claims)
        => new(claims.Select(claim => new Claim(claim.Type, claim.Value)), "foreign");

    [Fact]
    public void EveryPlatformAuthorityClaimIsRemoved()
    {
        var authority = ClaimDefinitions.All().Select(definition => definition.Name)
            .Where(name => name != ClaimDefinitions.sub.Name && name != ClaimDefinitions.jti.Name)
            .ToList();
        var identity = Identity([("sub", "person"), .. authority.Select(name => (name, "forged"))]);

        var removed = ForeignClaims.WithoutPlatformClaims(identity, AcceptsNothing);

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

        ForeignClaims.WithoutPlatformClaims(identity, AcceptsNothing).Count.ShouldBe(5);

        identity.Claims.ShouldBeEmpty();
    }

    [Fact]
    public void TheIssuersOwnSubjectAndTokenIdStay()
    {
        var identity = Identity((ClaimDefinitions.sub.Name, "person"), (ClaimDefinitions.jti.Name, "token-1"));

        ForeignClaims.WithoutPlatformClaims(identity, AcceptsNothing).ShouldBeEmpty();

        identity.FindFirst(ClaimDefinitions.sub.Name)?.Value.ShouldBe("person");
        identity.FindFirst(ClaimDefinitions.jti.Name)?.Value.ShouldBe("token-1");
    }

    [Fact]
    public void ClaimsThatAreNotThePlatformsStay()
    {
        var identity = Identity(("name", "Jo"), ("email", "jo@example.test"), ("iss", "https://issuer.test"), (ClaimDefinitions.perm.Name, "forged"));

        var removed = ForeignClaims.WithoutPlatformClaims(identity, AcceptsNothing);

        removed.ShouldBe(new[] { ClaimDefinitions.perm.Name });
        identity.Claims.Select(claim => claim.Type).ToArray().ShouldBe(new[] { "name", "email", "iss" });
    }

    [Fact]
    public void EveryValueOfARepeatedPlatformClaimIsRemoved()
    {
        var identity = Identity((ClaimDefinitions.perm.Name, "a"), (ClaimDefinitions.perm.Name, "b"), (ClaimDefinitions.perm.Name, "c"));

        ForeignClaims.WithoutPlatformClaims(identity, AcceptsNothing).Count.ShouldBe(3);

        identity.Claims.ShouldBeEmpty();
    }

    [Fact]
    public void AClaimTheEntryAcceptsStaysAndTheOtherPlatformClaimsAreRemoved()
    {
        var identity = Identity((ClaimDefinitions.roles.Name, "Operator"), (ClaimDefinitions.perm.Name, "forged"), (ClaimDefinitions.tenantId.Name, "t"));

        var removed = ForeignClaims.WithoutPlatformClaims(identity, [ClaimDefinitions.roles.Name]);

        removed.OrderBy(type => type, StringComparer.Ordinal).ToArray().ShouldBe(new[] { ClaimDefinitions.perm.Name, ClaimDefinitions.tenantId.Name });
        identity.Claims.Select(claim => claim.Type).ToArray().ShouldBe(new[] { ClaimDefinitions.roles.Name });
    }

    [Fact]
    public void EveryValueOfAnAcceptedClaimStays()
    {
        var identity = Identity((ClaimDefinitions.roles.Name, "A"), (ClaimDefinitions.roles.Name, "B"));

        ForeignClaims.WithoutPlatformClaims(identity, [ClaimDefinitions.roles.Name]).ShouldBeEmpty();

        identity.FindAll(ClaimDefinitions.roles.Name).Select(claim => claim.Value).ToArray().ShouldBe(new[] { "A", "B" });
    }
}
