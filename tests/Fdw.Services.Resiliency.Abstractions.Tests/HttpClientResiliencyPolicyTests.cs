using Fdw.Services.Resiliency.Abstractions.Policies;
using Shouldly;
using Xunit;

namespace Fdw.Services.Resiliency.Abstractions.Tests;

/// <summary>
/// Tests for HttpClientResiliencyPolicy.
/// </summary>
public class HttpClientResiliencyPolicyTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsIdCorrectly()
    {
        // Act
        var policy = new HttpClientResiliencyPolicy();

        // Assert
        policy.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsNameCorrectly()
    {
        // Act
        var policy = new HttpClientResiliencyPolicy();

        // Assert
        policy.Name.ShouldBe("HttpClient");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxRetriesReturnsFive()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Act
        var maxRetries = policy.MaxRetries;

        // Assert
        maxRetries.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InitialDelayReturnsTwoHundredMilliseconds()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Act
        var delay = policy.InitialDelay;

        // Assert
        delay.ShouldBe(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxDelayReturnsThirtySeconds()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Act
        var maxDelay = policy.MaxDelay;

        // Assert
        maxDelay.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BackoffMultiplierReturnsTwo()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Act
        var multiplier = policy.BackoffMultiplier;

        // Assert
        multiplier.ShouldBe(2.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerDurationReturnsSixtySeconds()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Act
        var duration = policy.CircuitBreakerDuration;

        // Assert
        duration.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerThresholdReturnsTen()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Act
        var threshold = policy.CircuitBreakerThreshold;

        // Assert
        threshold.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResiliencyCategoryReturnsHttpClient()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Act
        var category = policy.ResiliencyCategory;

        // Assert
        category.Name.ShouldBe("HttpClient");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIResiliencyPolicy()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InheritsFromResiliencyPolicyBase()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<ResiliencyPolicyBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSealed()
    {
        // Arrange
        var type = typeof(HttpClientResiliencyPolicy);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NamePropertyMatchesConstructorParameter()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Assert
        policy.Name.ShouldBe("HttpClient");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IdIsConsistentAcrossInstances()
    {
        // Arrange
        var policy1 = new HttpClientResiliencyPolicy();
        var policy2 = new HttpClientResiliencyPolicy();

        // Assert
        policy1.Id.ShouldBe(policy2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPropertiesReturnConsistentValues()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Act - Access properties multiple times
        var maxRetries1 = policy.MaxRetries;
        var maxRetries2 = policy.MaxRetries;
        var delay1 = policy.InitialDelay;
        var delay2 = policy.InitialDelay;

        // Assert
        maxRetries1.ShouldBe(maxRetries2);
        delay1.ShouldBe(delay2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HasHigherRetriesThanDatabasePolicy()
    {
        // Arrange
        var httpPolicy = new HttpClientResiliencyPolicy();
        var dbPolicy = new DatabaseResiliencyPolicy();

        // Assert
        httpPolicy.MaxRetries.ShouldBeGreaterThan(dbPolicy.MaxRetries);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HasLongerCircuitBreakerDurationThanDatabasePolicy()
    {
        // Arrange
        var httpPolicy = new HttpClientResiliencyPolicy();
        var dbPolicy = new DatabaseResiliencyPolicy();

        // Assert
        httpPolicy.CircuitBreakerDuration.ShouldBeGreaterThan(dbPolicy.CircuitBreakerDuration);
    }
}
