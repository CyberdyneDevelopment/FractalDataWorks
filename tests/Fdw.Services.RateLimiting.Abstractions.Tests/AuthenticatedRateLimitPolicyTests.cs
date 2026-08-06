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
    public void ConstructorSetsCorrectId()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.Id.ShouldBe(2);
    }

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
    public void RequestsPerWindowReturns500()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.RequestsPerWindow.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void WindowReturnsOneMinute()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.Window.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllowBurstReturnsTrue()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.AllowBurst.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitIs1Point5TimesNormalLimit()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.BurstLimit.ShouldBe(750);
        policy.BurstLimit.ShouldBe((int)(policy.RequestsPerWindow * 1.5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitExceedsRequestsPerWindow()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.BurstLimit.ShouldBeGreaterThan(policy.RequestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AlgorithmReturnsSlidingWindow()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.Algorithm.Name.ShouldBe("SlidingWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SegmentsPerWindowReturns10()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.SegmentsPerWindow.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void QueueExceededRequestsReturnsFalse()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.QueueExceededRequests.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void QueueLimitReturnsZero()
    {
        // Act
        var policy = new AuthenticatedRateLimitPolicy();

        // Assert
        policy.QueueLimit.ShouldBe(0);
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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequestsPerWindowExceedsStandardPolicy()
    {
        // Arrange
        var standard = new StandardRateLimitPolicy();
        var authenticated = new AuthenticatedRateLimitPolicy();

        // Assert
        authenticated.RequestsPerWindow.ShouldBeGreaterThan(standard.RequestsPerWindow);
    }
}
