using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Abstractions.Policies;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for PremiumRateLimitPolicy.
/// Verifies the high-volume rate limiting configuration for premium tier users with token bucket algorithm.
/// </summary>
public class PremiumRateLimitPolicyTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsCorrectName()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.Name.ShouldBe("Premium");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIRateLimitPolicy()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IRateLimitPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ExtendsRateLimitPolicyBase()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.ShouldBeAssignableTo<RateLimitPolicyBase>();
    }
}
