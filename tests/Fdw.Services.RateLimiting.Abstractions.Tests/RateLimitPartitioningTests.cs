using System;
using System.Net;
using System.Security.Claims;
using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for the ways a policy divides requests into buckets.
/// </summary>
[Collection(nameof(RateLimitingTestCollection))]
public class RateLimitPartitioningTests
{
    private static ClaimsPrincipal Caller(string id) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id) }, "test"));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PerCallerGivesEachCallerItsOwnKey()
    {
        var partitioning = RateLimitPartitionings.PerCaller;

        var first = partitioning.KeyOf(RateLimitPolicies.Authenticated, Caller("a"), null);
        var again = partitioning.KeyOf(RateLimitPolicies.Authenticated, Caller("a"), null);
        var other = partitioning.KeyOf(RateLimitPolicies.Authenticated, Caller("b"), null);

        first.ShouldBe(again);
        first.ShouldNotBe(other);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PerCallerRefusesARequestWithNoAuthenticatedCaller()
    {
        var partitioning = RateLimitPartitionings.PerCaller;

        Should.Throw<InvalidOperationException>(() => partitioning.KeyOf(RateLimitPolicies.Authenticated, null, null));
        Should.Throw<InvalidOperationException>(() => partitioning.KeyOf(RateLimitPolicies.Authenticated, new ClaimsPrincipal(new ClaimsIdentity()), null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PerCallerRefusesACallerWithNoIdClaim()
    {
        var partitioning = RateLimitPartitionings.PerCaller;
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "test"));

        Should.Throw<InvalidOperationException>(() => partitioning.KeyOf(RateLimitPolicies.Authenticated, anonymous, null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PerCallerReadsTheIdFromTheSubClaimWhenTheTokenWasNotMapped()
    {
        var unmapped = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "a") }, "test"));

        RateLimitPartitionings.PerCaller.KeyOf(RateLimitPolicies.Authenticated, unmapped, null).ShouldBe("a");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PerAddressGivesEachAddressItsOwnKey()
    {
        var partitioning = RateLimitPartitionings.PerAddress;

        var first = partitioning.KeyOf(RateLimitPolicies.Standard, null, IPAddress.Parse("10.0.0.1"));
        var other = partitioning.KeyOf(RateLimitPolicies.Standard, null, IPAddress.Parse("10.0.0.2"));

        first.ShouldBe("10.0.0.1");
        first.ShouldNotBe(other);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PerAddressRefusesARequestWithNoAddress()
    {
        Should.Throw<InvalidOperationException>(() => RateLimitPartitionings.PerAddress.KeyOf(RateLimitPolicies.Standard, null, null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SharedGivesEveryRequestTheSamePolicyKey()
    {
        var partitioning = RateLimitPartitionings.Shared;

        var first = partitioning.KeyOf(RateLimitPolicies.Admin, Caller("a"), IPAddress.Parse("10.0.0.1"));
        var other = partitioning.KeyOf(RateLimitPolicies.Admin, Caller("b"), IPAddress.Parse("10.0.0.2"));

        first.ShouldBe("Admin");
        other.ShouldBe(first);
    }
}
