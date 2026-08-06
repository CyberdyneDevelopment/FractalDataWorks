using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests.Conditions;

/// <summary>
/// Tests for RetryThresholdCondition evaluation behavior.
/// </summary>
public class RetryThresholdConditionTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsFalseWhenThresholdIsNull()
    {
        // Arrange
        var condition = new RetryThresholdCondition();
        var context = new NotificationContext
        {
            RetryCount = 5,
            Threshold = null
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsTrueWhenRetryCountMeetsThreshold()
    {
        // Arrange
        var condition = new RetryThresholdCondition();
        var context = new NotificationContext
        {
            RetryCount = 5,
            Threshold = 5
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsTrueWhenRetryCountExceedsThreshold()
    {
        // Arrange
        var condition = new RetryThresholdCondition();
        var context = new NotificationContext
        {
            RetryCount = 10,
            Threshold = 5
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsFalseWhenRetryCountBelowThreshold()
    {
        // Arrange
        var condition = new RetryThresholdCondition();
        var context = new NotificationContext
        {
            RetryCount = 3,
            Threshold = 5
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateRespectsNegation()
    {
        // Arrange
        var condition = new RetryThresholdCondition();
        var context = new NotificationContext
        {
            RetryCount = 10,
            Threshold = 5,
            IsNegated = true
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse(); // Negated: 10 >= 5 is true, but negated becomes false
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConditionHasCorrectMetadata()
    {
        // Arrange & Act
        var condition = new RetryThresholdCondition();

        // Assert
        condition.Id.ShouldBe(1);
        condition.Name.ShouldBe("RetryThreshold");
        condition.Icon.ShouldBe("replay");
        condition.Color.ShouldBe("Warning");
    }
}
