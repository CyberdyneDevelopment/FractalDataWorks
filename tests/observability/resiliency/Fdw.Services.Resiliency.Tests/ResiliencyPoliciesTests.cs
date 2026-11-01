using System;
using Xunit;
using Shouldly;
using Fdw.Services.Resiliency.Abstractions;

namespace Fdw.Services.Resiliency.Tests;

/// <summary>
/// Unit tests for ResiliencyPolicies TypeCollection lookups.
/// </summary>
[Collection(nameof(ResiliencyTestCollection))]
public sealed class ResiliencyPoliciesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DatabasePolicyHasCorrectConfiguration()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("Database");

        // Assert
        policy.MaxRetries.ShouldBe(3);
        policy.InitialDelay.ShouldBe(TimeSpan.FromMilliseconds(100));
        policy.MaxDelay.ShouldBe(TimeSpan.FromSeconds(5));
        policy.BackoffMultiplier.ShouldBe(2.0);
        policy.CircuitBreakerDuration.ShouldBe(TimeSpan.FromSeconds(30));
        policy.CircuitBreakerThreshold.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HttpClientPolicyHasCorrectConfiguration()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("HttpClient");

        // Assert
        policy.MaxRetries.ShouldBe(5);
        policy.InitialDelay.ShouldBe(TimeSpan.FromMilliseconds(200));
        policy.MaxDelay.ShouldBe(TimeSpan.FromSeconds(30));
        policy.BackoffMultiplier.ShouldBe(2.0);
        policy.CircuitBreakerDuration.ShouldBe(TimeSpan.FromSeconds(60));
        policy.CircuitBreakerThreshold.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CriticalPolicyHasAggressiveRetryConfiguration()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("Critical");

        // Assert
        policy.MaxRetries.ShouldBe(10);
        policy.CircuitBreakerThreshold.ShouldBeGreaterThan(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SimplePolicyHasMinimalConfiguration()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("Simple");

        // Assert
        policy.MaxRetries.ShouldBeLessThanOrEqualTo(3);
        policy.InitialDelay.TotalMilliseconds.ShouldBeLessThan(200);
    }
}
