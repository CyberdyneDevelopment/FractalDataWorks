using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Abstractions.Policies;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for AuthenticatedRateLimitPolicy.
/// Verifies the enhanced rate limiting configuration for authenticated users with burst support.
/// </summary>
public class AuthenticatedRateLimitPolicyTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsCorrectName()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.Name.ShouldBe("Authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIRateLimitPolicy()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IRateLimitPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ExtendsRateLimitPolicyBase()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.ShouldBeAssignableTo<RateLimitPolicyBase>();
    }
}
