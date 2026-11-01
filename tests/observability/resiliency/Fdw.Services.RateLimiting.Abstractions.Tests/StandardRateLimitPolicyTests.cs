using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Abstractions.Policies;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for StandardRateLimitPolicy.
/// Verifies the conservative rate limiting configuration for standard/unauthenticated users.
/// </summary>
public class StandardRateLimitPolicyTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsCorrectName()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.Name.ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIRateLimitPolicy()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IRateLimitPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ExtendsRateLimitPolicyBase()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.ShouldBeAssignableTo<RateLimitPolicyBase>();
    }
}
