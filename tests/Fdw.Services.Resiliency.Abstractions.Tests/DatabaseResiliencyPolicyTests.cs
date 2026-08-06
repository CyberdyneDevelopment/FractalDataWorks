using Fdw.Services.Resiliency.Abstractions.Policies;
using Shouldly;
using Xunit;

namespace Fdw.Services.Resiliency.Abstractions.Tests;

/// <summary>
/// Tests for DatabaseResiliencyPolicy.
/// </summary>
public class DatabaseResiliencyPolicyTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsIdCorrectly()
    {
        // Act
        var policy = new DatabaseResiliencyPolicy();

        // Assert
        policy.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsNameCorrectly()
    {
        // Act
        var policy = new DatabaseResiliencyPolicy();

        // Assert
        policy.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxRetriesReturnsThree()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Act
        var maxRetries = policy.MaxRetries;

        // Assert
        maxRetries.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InitialDelayReturnsOneHundredMilliseconds()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Act
        var delay = policy.InitialDelay;

        // Assert
        delay.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxDelayReturnsFiveSeconds()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Act
        var maxDelay = policy.MaxDelay;

        // Assert
        maxDelay.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BackoffMultiplierReturnsTwo()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Act
        var multiplier = policy.BackoffMultiplier;

        // Assert
        multiplier.ShouldBe(2.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerDurationReturnsThirtySeconds()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Act
        var duration = policy.CircuitBreakerDuration;

        // Assert
        duration.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerThresholdReturnsFive()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Act
        var threshold = policy.CircuitBreakerThreshold;

        // Assert
        threshold.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResiliencyCategoryReturnsDatabase()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Act
        var category = policy.ResiliencyCategory;

        // Assert
        category.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIResiliencyPolicy()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InheritsFromResiliencyPolicyBase()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<ResiliencyPolicyBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsSealed()
    {
        // Arrange
        var type = typeof(DatabaseResiliencyPolicy);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NamePropertyMatchesConstructorParameter()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Assert
        policy.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IdIsConsistentAcrossInstances()
    {
        // Arrange
        var policy1 = new DatabaseResiliencyPolicy();
        var policy2 = new DatabaseResiliencyPolicy();

        // Assert
        policy1.Id.ShouldBe(policy2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllPropertiesReturnConsistentValues()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Act - Access properties multiple times
        var maxRetries1 = policy.MaxRetries;
        var maxRetries2 = policy.MaxRetries;
        var delay1 = policy.InitialDelay;
        var delay2 = policy.InitialDelay;

        // Assert
        maxRetries1.ShouldBe(maxRetries2);
        delay1.ShouldBe(delay2);
    }
}
