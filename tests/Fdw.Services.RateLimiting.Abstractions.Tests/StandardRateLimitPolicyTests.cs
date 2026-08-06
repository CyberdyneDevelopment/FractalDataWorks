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
    public void ConstructorSetsCorrectId()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.Id.ShouldBe(1);
    }

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
    public void RequestsPerWindowReturns100()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.RequestsPerWindow.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void WindowReturnsOneMinute()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.Window.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllowBurstReturnsFalse()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.AllowBurst.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BurstLimitEqualsRequestsPerWindow()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.BurstLimit.ShouldBe(policy.RequestsPerWindow);
        policy.BurstLimit.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AlgorithmReturnsSlidingWindow()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.Algorithm.Name.ShouldBe("SlidingWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SegmentsPerWindowReturns10()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.SegmentsPerWindow.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void QueueExceededRequestsReturnsFalse()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.QueueExceededRequests.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void QueueLimitReturnsZero()
    {
        // Act
        var policy = new StandardRateLimitPolicy();

        // Assert
        policy.QueueLimit.ShouldBe(0);
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
