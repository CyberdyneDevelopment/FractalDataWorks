using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Abstractions.Policies;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for AdminRateLimitPolicy.
/// Verifies the highest-tier rate limiting configuration for administrative access.
/// </summary>
public class AdminRateLimitPolicyTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsCorrectId()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsCorrectName()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.Name.ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequestsPerWindowReturns10000()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.RequestsPerWindow.ShouldBe(10000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void WindowReturnsOneMinute()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.Window.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllowBurstReturnsTrue()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.AllowBurst.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitIs1Point5TimesNormalLimit()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.BurstLimit.ShouldBe(15000);
        policy.BurstLimit.ShouldBe((int)(policy.RequestsPerWindow * 1.5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitExceedsRequestsPerWindow()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.BurstLimit.ShouldBeGreaterThan(policy.RequestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AlgorithmReturnsTokenBucket()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.Algorithm.Name.ShouldBe("TokenBucket");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SegmentsPerWindowReturns1()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.SegmentsPerWindow.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void QueueExceededRequestsReturnsFalse()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.QueueExceededRequests.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void QueueLimitReturnsZero()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.QueueLimit.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIRateLimitPolicy()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IRateLimitPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ExtendsRateLimitPolicyBase()
    {
        // Act
        var policy = new AdminRateLimitPolicy();

        // Assert
        policy.ShouldBeAssignableTo<RateLimitPolicyBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequestsPerWindowExceedsPremiumPolicy()
    {
        // Arrange
        var premium = new PremiumRateLimitPolicy();
        var admin = new AdminRateLimitPolicy();

        // Assert
        admin.RequestsPerWindow.ShouldBeGreaterThan(premium.RequestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void UsesTokenBucketAlgorithm()
    {
        // Arrange
        var premium = new PremiumRateLimitPolicy();
        var admin = new AdminRateLimitPolicy();

        // Assert
        admin.Algorithm.Name.ShouldBe("TokenBucket");
        admin.Algorithm.Name.ShouldBe(premium.Algorithm.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HasHighestRequestLimit()
    {
        // Arrange
        var standard = new StandardRateLimitPolicy();
        var authenticated = new AuthenticatedRateLimitPolicy();
        var premium = new PremiumRateLimitPolicy();
        var admin = new AdminRateLimitPolicy();

        // Assert
        admin.RequestsPerWindow.ShouldBeGreaterThan(standard.RequestsPerWindow);
        admin.RequestsPerWindow.ShouldBeGreaterThan(authenticated.RequestsPerWindow);
        admin.RequestsPerWindow.ShouldBeGreaterThan(premium.RequestsPerWindow);
    }
}
