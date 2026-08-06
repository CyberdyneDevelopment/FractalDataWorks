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
    public void ConstructorSetsCorrectId()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.Id.ShouldBe(3);
    }

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
    public void RequestsPerWindowReturns2000()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.RequestsPerWindow.ShouldBe(2000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void WindowReturnsOneMinute()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.Window.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllowBurstReturnsTrue()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.AllowBurst.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitIs1Point5TimesNormalLimit()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.BurstLimit.ShouldBe(3000);
        policy.BurstLimit.ShouldBe((int)(policy.RequestsPerWindow * 1.5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitExceedsRequestsPerWindow()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.BurstLimit.ShouldBeGreaterThan(policy.RequestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AlgorithmReturnsTokenBucket()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.Algorithm.Name.ShouldBe("TokenBucket");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SegmentsPerWindowReturns1()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.SegmentsPerWindow.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void QueueExceededRequestsReturnsFalse()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.QueueExceededRequests.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void QueueLimitReturnsZero()
    {
        // Act
        var policy = new PremiumRateLimitPolicy();

        // Assert
        policy.QueueLimit.ShouldBe(0);
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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequestsPerWindowExceedsAuthenticatedPolicy()
    {
        // Arrange
        var authenticated = new AuthenticatedRateLimitPolicy();
        var premium = new PremiumRateLimitPolicy();

        // Assert
        premium.RequestsPerWindow.ShouldBeGreaterThan(authenticated.RequestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void UsesTokenBucketAlgorithm()
    {
        // Arrange
        var authenticated = new AuthenticatedRateLimitPolicy();
        var premium = new PremiumRateLimitPolicy();

        // Assert
        premium.Algorithm.Name.ShouldBe("TokenBucket");
        premium.Algorithm.Name.ShouldNotBe(authenticated.Algorithm.Name);
    }
}
