using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for RateLimitPolicies TypeCollection.
/// Verifies that all policies are discovered and accessible via the collection.
/// </summary>
[Collection(nameof(RateLimitingTestCollection))]
public class RateLimitPoliciesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardPropertyReturnsStandardPolicy()
    {
        // Act
        var policy = RateLimitPolicies.Standard;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AuthenticatedPropertyReturnsAuthenticatedPolicy()
    {
        // Act
        var policy = RateLimitPolicies.Authenticated;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PremiumPropertyReturnsPremiumPolicy()
    {
        // Act
        var policy = RateLimitPolicies.Premium;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Premium");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AdminPropertyReturnsAdminPolicy()
    {
        // Act
        var policy = RateLimitPolicies.Admin;

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHavePositiveIds()
    {
        // Act
        var all = RateLimitPolicies.All();

        // Assert
        all.ShouldAllBe(p => p.Id > 0);
    }
}
