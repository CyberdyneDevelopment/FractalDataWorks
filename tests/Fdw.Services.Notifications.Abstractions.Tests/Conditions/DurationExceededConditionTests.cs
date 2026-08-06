using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests.Conditions;

/// <summary>
/// Tests for DurationExceededCondition evaluation behavior.
/// </summary>
public class DurationExceededConditionTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsFalseWhenDurationTicksIsNull()
    {
        // Arrange
        var condition = new DurationExceededCondition();
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(10),
            DurationTicks = null
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
    public void EvaluateReturnsTrueWhenDurationExceedsThreshold()
    {
        // Arrange
        var condition = new DurationExceededCondition();
        var threshold = TimeSpan.FromMinutes(5);
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(10),
            DurationTicks = threshold.Ticks
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
    public void EvaluateReturnsFalseWhenDurationBelowThreshold()
    {
        // Arrange
        var condition = new DurationExceededCondition();
        var threshold = TimeSpan.FromMinutes(10);
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(5),
            DurationTicks = threshold.Ticks
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
    public void EvaluateReturnsFalseWhenDurationEqualsThreshold()
    {
        // Arrange
        var condition = new DurationExceededCondition();
        var threshold = TimeSpan.FromMinutes(5);
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(5),
            DurationTicks = threshold.Ticks
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
        var condition = new DurationExceededCondition();
        var threshold = TimeSpan.FromMinutes(5);
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(10),
            DurationTicks = threshold.Ticks,
            IsNegated = true
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse(); // Negated: 10 > 5 is true, but negated becomes false
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConditionHasCorrectMetadata()
    {
        // Arrange & Act
        var condition = new DurationExceededCondition();

        // Assert
        condition.Id.ShouldBe(3);
        condition.Name.ShouldBe("DurationExceeded");
        condition.Icon.ShouldBe("timer_off");
        condition.Color.ShouldBe("Warning");
    }
}
