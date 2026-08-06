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
    public void ByNameDatabaseReturnsPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("Database");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Database");
        policy.ResiliencyCategory.ShouldBe(ResiliencyCategories.Database);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameHttpClientReturnsPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("HttpClient");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("HttpClient");
        policy.ResiliencyCategory.ShouldBe(ResiliencyCategories.HttpClient);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameCriticalReturnsPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("Critical");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Critical");
        policy.ResiliencyCategory.ShouldBe(ResiliencyCategories.Critical);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameSimpleReturnsPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("Simple");

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Simple");
        policy.ResiliencyCategory.ShouldBe(ResiliencyCategories.Simple);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsEmptyForUnknownPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.ByName("NonExistent");

        // Assert
        policy.ShouldBe(ResiliencyPolicies.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsCorrectPolicy()
    {
        // Act
        var policy = ResiliencyPolicies.ById(1); // Database is ID 1

        // Assert
        policy.ShouldNotBeNull();
        policy.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CountReturnsExpectedNumberOfPolicies()
    {
        // Assert
        ResiliencyPolicies.All().Count.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsPolicies()
    {
        // Act
        var policies = ResiliencyPolicies.All();

        // Assert
        policies.ShouldNotBeNull();
        policies.ShouldNotBeEmpty();
    }

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
