using Fdw.Services.Resiliency.Abstractions.Policies;
using Shouldly;
using Xunit;

namespace Fdw.Services.Resiliency.Abstractions.Tests;

/// <summary>
/// Tests for SimpleRetryResiliencyPolicy.
/// </summary>
public class SimpleRetryResiliencyPolicyTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsIdCorrectly()
    {
        // Act
        var policy = new SimpleRetryResiliencyPolicy();

        // Assert
        policy.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsNameCorrectly()
    {
        // Act
        var policy = new SimpleRetryResiliencyPolicy();

        // Assert
        policy.Name.ShouldBe("Simple");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxRetriesReturnsTwo()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Act
        var maxRetries = policy.MaxRetries;

        // Assert
        maxRetries.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InitialDelayReturnsFiftyMilliseconds()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Act
        var delay = policy.InitialDelay;

        // Assert
        delay.ShouldBe(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxDelayReturnsFiveHundredMilliseconds()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Act
        var maxDelay = policy.MaxDelay;

        // Assert
        maxDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BackoffMultiplierReturnsTwo()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Act
        var multiplier = policy.BackoffMultiplier;

        // Assert
        multiplier.ShouldBe(2.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerDurationReturnsFifteenSeconds()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Act
        var duration = policy.CircuitBreakerDuration;

        // Assert
        duration.ShouldBe(TimeSpan.FromSeconds(15));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerThresholdReturnsThree()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Act
        var threshold = policy.CircuitBreakerThreshold;

        // Assert
        threshold.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResiliencyCategoryReturnsSimple()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Act
        var category = policy.ResiliencyCategory;

        // Assert
        category.Name.ShouldBe("Simple");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIResiliencyPolicy()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InheritsFromResiliencyPolicyBase()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<ResiliencyPolicyBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSealed()
    {
        // Arrange
        var type = typeof(SimpleRetryResiliencyPolicy);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NamePropertyMatchesConstructorParameter()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Assert
        policy.Name.ShouldBe("Simple");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IdIsConsistentAcrossInstances()
    {
        // Arrange
        var policy1 = new SimpleRetryResiliencyPolicy();
        var policy2 = new SimpleRetryResiliencyPolicy();

        // Assert
        policy1.Id.ShouldBe(policy2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPropertiesReturnConsistentValues()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

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
    public void HasLowestMaxRetriesOfAllPolicies()
    {
        // Arrange
        var simple = new SimpleRetryResiliencyPolicy();
        var database = new DatabaseResiliencyPolicy();
        var httpClient = new HttpClientResiliencyPolicy();
        var critical = new CriticalResiliencyPolicy();

        // Assert
        simple.MaxRetries.ShouldBeLessThan(database.MaxRetries);
        simple.MaxRetries.ShouldBeLessThan(httpClient.MaxRetries);
        simple.MaxRetries.ShouldBeLessThan(critical.MaxRetries);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HasShortestInitialDelayOfAllPolicies()
    {
        // Arrange
        var simple = new SimpleRetryResiliencyPolicy();
        var database = new DatabaseResiliencyPolicy();
        var httpClient = new HttpClientResiliencyPolicy();
        var critical = new CriticalResiliencyPolicy();

        // Assert
        simple.InitialDelay.ShouldBeLessThan(database.InitialDelay);
        simple.InitialDelay.ShouldBeLessThan(httpClient.InitialDelay);
        simple.InitialDelay.ShouldBeLessThan(critical.InitialDelay);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HasShortestCircuitBreakerDurationOfAllPolicies()
    {
        // Arrange
        var simple = new SimpleRetryResiliencyPolicy();
        var database = new DatabaseResiliencyPolicy();
        var httpClient = new HttpClientResiliencyPolicy();
        var critical = new CriticalResiliencyPolicy();

        // Assert
        simple.CircuitBreakerDuration.ShouldBeLessThan(database.CircuitBreakerDuration);
        simple.CircuitBreakerDuration.ShouldBeLessThan(httpClient.CircuitBreakerDuration);
        simple.CircuitBreakerDuration.ShouldBeLessThan(critical.CircuitBreakerDuration);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HasLowestCircuitBreakerThresholdOfAllPolicies()
    {
        // Arrange
        var simple = new SimpleRetryResiliencyPolicy();
        var database = new DatabaseResiliencyPolicy();
        var httpClient = new HttpClientResiliencyPolicy();
        var critical = new CriticalResiliencyPolicy();

        // Assert
        simple.CircuitBreakerThreshold.ShouldBeLessThan(database.CircuitBreakerThreshold);
        simple.CircuitBreakerThreshold.ShouldBeLessThan(httpClient.CircuitBreakerThreshold);
        simple.CircuitBreakerThreshold.ShouldBeLessThan(critical.CircuitBreakerThreshold);
    }
}
