using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests.Conditions;

/// <summary>
/// Tests for ConsecutiveFailuresCondition evaluation behavior.
/// </summary>
public class ConsecutiveFailuresConditionTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsFalseWhenThresholdIsNull()
    {
        // Arrange
        var condition = new ConsecutiveFailuresCondition();
        var context = new NotificationContext
        {
            ConsecutiveFailures = 5,
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
    public void EvaluateReturnsTrueWhenConsecutiveFailuresMeetsThreshold()
    {
        // Arrange
        var condition = new ConsecutiveFailuresCondition();
        var context = new NotificationContext
        {
            ConsecutiveFailures = 5,
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
    public void EvaluateReturnsTrueWhenConsecutiveFailuresExceedsThreshold()
    {
        // Arrange
        var condition = new ConsecutiveFailuresCondition();
        var context = new NotificationContext
        {
            ConsecutiveFailures = 10,
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
    public void EvaluateReturnsFalseWhenConsecutiveFailuresBelowThreshold()
    {
        // Arrange
        var condition = new ConsecutiveFailuresCondition();
        var context = new NotificationContext
        {
            ConsecutiveFailures = 3,
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
        var condition = new ConsecutiveFailuresCondition();
        var context = new NotificationContext
        {
            ConsecutiveFailures = 10,
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
        var condition = new ConsecutiveFailuresCondition();

        // Assert
        condition.Id.ShouldBe(2);
        condition.Name.ShouldBe("ConsecutiveFailures");
        condition.Icon.ShouldBe("error_outline");
        condition.Color.ShouldBe("Error");
    }
}
