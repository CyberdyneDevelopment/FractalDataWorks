using Fdw.Services.Resiliency.Abstractions.Policies;
using Shouldly;
using Xunit;

namespace Fdw.Services.Resiliency.Abstractions.Tests;

/// <summary>
/// Tests for CriticalResiliencyPolicy.
/// </summary>
public class CriticalResiliencyPolicyTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsIdCorrectly()
    {
        // Act
        var policy = new CriticalResiliencyPolicy();

        // Assert
        policy.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsNameCorrectly()
    {
        // Act
        var policy = new CriticalResiliencyPolicy();

        // Assert
        policy.Name.ShouldBe("Critical");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxRetriesReturnsTen()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Act
        var maxRetries = policy.MaxRetries;

        // Assert
        maxRetries.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InitialDelayReturnsFiveHundredMilliseconds()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Act
        var delay = policy.InitialDelay;

        // Assert
        delay.ShouldBe(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxDelayReturnsTwoMinutes()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Act
        var maxDelay = policy.MaxDelay;

        // Assert
        maxDelay.ShouldBe(TimeSpan.FromMinutes(2));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BackoffMultiplierReturnsOnePointFive()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Act
        var multiplier = policy.BackoffMultiplier;

        // Assert
        multiplier.ShouldBe(1.5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerDurationReturnsFiveMinutes()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Act
        var duration = policy.CircuitBreakerDuration;

        // Assert
        duration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerThresholdReturnsTwenty()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Act
        var threshold = policy.CircuitBreakerThreshold;

        // Assert
        threshold.ShouldBe(20);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResiliencyCategoryReturnsCritical()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Act
        var category = policy.ResiliencyCategory;

        // Assert
        category.Name.ShouldBe("Critical");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIResiliencyPolicy()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InheritsFromResiliencyPolicyBase()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<ResiliencyPolicyBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSealed()
    {
        // Arrange
        var type = typeof(CriticalResiliencyPolicy);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NamePropertyMatchesConstructorParameter()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Assert
        policy.Name.ShouldBe("Critical");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IdIsConsistentAcrossInstances()
    {
        // Arrange
        var policy1 = new CriticalResiliencyPolicy();
        var policy2 = new CriticalResiliencyPolicy();

        // Assert
        policy1.Id.ShouldBe(policy2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPropertiesReturnConsistentValues()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

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
    public void HasHighestMaxRetriesOfAllPolicies()
    {
        // Arrange
        var critical = new CriticalResiliencyPolicy();
        var database = new DatabaseResiliencyPolicy();
        var httpClient = new HttpClientResiliencyPolicy();
        var simple = new SimpleRetryResiliencyPolicy();

        // Assert
        critical.MaxRetries.ShouldBeGreaterThan(database.MaxRetries);
        critical.MaxRetries.ShouldBeGreaterThan(httpClient.MaxRetries);
        critical.MaxRetries.ShouldBeGreaterThan(simple.MaxRetries);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HasLongestCircuitBreakerDurationOfAllPolicies()
    {
        // Arrange
        var critical = new CriticalResiliencyPolicy();
        var database = new DatabaseResiliencyPolicy();
        var httpClient = new HttpClientResiliencyPolicy();
        var simple = new SimpleRetryResiliencyPolicy();

        // Assert
        critical.CircuitBreakerDuration.ShouldBeGreaterThan(database.CircuitBreakerDuration);
        critical.CircuitBreakerDuration.ShouldBeGreaterThan(httpClient.CircuitBreakerDuration);
        critical.CircuitBreakerDuration.ShouldBeGreaterThan(simple.CircuitBreakerDuration);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HasHighestCircuitBreakerThresholdOfAllPolicies()
    {
        // Arrange
        var critical = new CriticalResiliencyPolicy();
        var database = new DatabaseResiliencyPolicy();
        var httpClient = new HttpClientResiliencyPolicy();
        var simple = new SimpleRetryResiliencyPolicy();

        // Assert
        critical.CircuitBreakerThreshold.ShouldBeGreaterThan(database.CircuitBreakerThreshold);
        critical.CircuitBreakerThreshold.ShouldBeGreaterThan(httpClient.CircuitBreakerThreshold);
        critical.CircuitBreakerThreshold.ShouldBeGreaterThan(simple.CircuitBreakerThreshold);
    }
}
