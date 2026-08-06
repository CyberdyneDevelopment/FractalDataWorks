using System;
using Xunit;
using Shouldly;
using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.RateLimiting.Tests;

/// <summary>
/// Unit tests for RateLimitPolicies TypeCollection lookups.
/// </summary>
public sealed class RateLimitPoliciesTests
{
    // =========================================================================
    // ByName Lookup Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameStandardReturnsPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Standard");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameAuthenticatedReturnsPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Authenticated");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNamePremiumReturnsPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Premium");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Premium");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameAdminReturnsPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Admin");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsEmptyForUnknownPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ByName("NonExistent");

        // Assert
        policy.ShouldBe(RateLimitPolicies.NotFound);
    }

    // =========================================================================
    // ById Lookup Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsStandardPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ById(1);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsAuthenticatedPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ById(2);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsPremiumPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ById(3);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Premium");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsAdminPolicy()
    {
        // Act
        var policy = RateLimitPolicies.ById(4);

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Admin");
    }

    // =========================================================================
    // All() Enumeration Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsAllPolicies()
    {
        // Act
        var policies = RateLimitPolicies.All();

        // Assert
        policies.ShouldNotBeNull();
        policies.ShouldNotBeEmpty();
        policies.Count.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CountReturnsExpectedNumberOfPolicies()
    {
        // Assert
        RateLimitPolicies.All().Count.ShouldBe(4);
    }

    // =========================================================================
    // Standard Policy Configuration Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardPolicyHasCorrectRequestLimit()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Standard");

        // Assert
        policy.RequestsPerWindow.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardPolicyHasOneMinuteWindow()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Standard");

        // Assert
        policy.Window.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardPolicyDoesNotAllowBurst()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Standard");

        // Assert
        policy.AllowBurst.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardPolicyUsesSlidingWindowAlgorithm()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Standard");

        // Assert
        policy.Algorithm.Name.ShouldBe("SlidingWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StandardPolicyDoesNotQueueRequests()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Standard");

        // Assert
        policy.QueueExceededRequests.ShouldBeFalse();
        policy.QueueLimit.ShouldBe(0);
    }

    // =========================================================================
    // Authenticated Policy Configuration Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AuthenticatedPolicyHasCorrectRequestLimit()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Authenticated");

        // Assert
        policy.RequestsPerWindow.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AuthenticatedPolicyAllowsBurst()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Authenticated");

        // Assert
        policy.AllowBurst.ShouldBeTrue();
        policy.BurstLimit.ShouldBe(750);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AuthenticatedPolicyHasHigherLimitThanStandard()
    {
        // Act
        var standard = RateLimitPolicies.ByName("Standard");
        var authenticated = RateLimitPolicies.ByName("Authenticated");

        // Assert
        authenticated.RequestsPerWindow.ShouldBeGreaterThan(standard.RequestsPerWindow);
    }

    // =========================================================================
    // Premium Policy Configuration Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PremiumPolicyHasCorrectRequestLimit()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Premium");

        // Assert
        policy.RequestsPerWindow.ShouldBe(2000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PremiumPolicyUsesTokenBucketAlgorithm()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Premium");

        // Assert
        policy.Algorithm.Name.ShouldBe("TokenBucket");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PremiumPolicyHasBurstCapability()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Premium");

        // Assert
        policy.AllowBurst.ShouldBeTrue();
        policy.BurstLimit.ShouldBe(3000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PremiumPolicyHasHigherLimitThanAuthenticated()
    {
        // Act
        var authenticated = RateLimitPolicies.ByName("Authenticated");
        var premium = RateLimitPolicies.ByName("Premium");

        // Assert
        premium.RequestsPerWindow.ShouldBeGreaterThan(authenticated.RequestsPerWindow);
    }

    // =========================================================================
    // Admin Policy Configuration Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AdminPolicyHasCorrectRequestLimit()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Admin");

        // Assert
        policy.RequestsPerWindow.ShouldBe(10000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AdminPolicyUsesTokenBucketAlgorithm()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Admin");

        // Assert
        policy.Algorithm.Name.ShouldBe("TokenBucket");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AdminPolicyHasGenerousBurstLimit()
    {
        // Act
        var policy = RateLimitPolicies.ByName("Admin");

        // Assert
        policy.AllowBurst.ShouldBeTrue();
        policy.BurstLimit.ShouldBe(15000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AdminPolicyHasHighestLimit()
    {
        // Act
        var policies = RateLimitPolicies.All();
        var admin = RateLimitPolicies.ByName("Admin");

        // Assert
        foreach (var policy in policies)
        {
            admin.RequestsPerWindow.ShouldBeGreaterThanOrEqualTo(policy.RequestsPerWindow);
        }
    }

    // =========================================================================
    // Policy Hierarchy Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PolicyTiersHaveIncreasingLimits()
    {
        // Arrange
        var standard = RateLimitPolicies.ByName("Standard");
        var authenticated = RateLimitPolicies.ByName("Authenticated");
        var premium = RateLimitPolicies.ByName("Premium");
        var admin = RateLimitPolicies.ByName("Admin");

        // Assert - limits increase with tier
        standard.RequestsPerWindow.ShouldBeLessThan(authenticated.RequestsPerWindow);
        authenticated.RequestsPerWindow.ShouldBeLessThan(premium.RequestsPerWindow);
        premium.RequestsPerWindow.ShouldBeLessThan(admin.RequestsPerWindow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPoliciesHaveOneMinuteWindow()
    {
        // Arrange
        var expectedWindow = TimeSpan.FromMinutes(1);

        // Act
        var policies = RateLimitPolicies.All();

        // Assert
        foreach (var policy in policies)
        {
            policy.Window.ShouldBe(expectedWindow);
        }
    }

    // =========================================================================
    // Algorithm Distribution Tests
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BasicPoliciesUseSlidingWindow()
    {
        // Arrange
        var standard = RateLimitPolicies.ByName("Standard");
        var authenticated = RateLimitPolicies.ByName("Authenticated");

        // Assert
        standard.Algorithm.Name.ShouldBe("SlidingWindow");
        authenticated.Algorithm.Name.ShouldBe("SlidingWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HighVolumePoliciesUseTokenBucket()
    {
        // Arrange
        var premium = RateLimitPolicies.ByName("Premium");
        var admin = RateLimitPolicies.ByName("Admin");

        // Assert
        premium.Algorithm.Name.ShouldBe("TokenBucket");
        admin.Algorithm.Name.ShouldBe("TokenBucket");
    }
}
