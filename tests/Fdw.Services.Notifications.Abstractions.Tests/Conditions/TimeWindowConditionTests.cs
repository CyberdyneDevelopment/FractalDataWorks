using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests.Conditions;

/// <summary>
/// Tests for TimeWindowCondition evaluation behavior.
/// </summary>
public class TimeWindowConditionTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsFalseWhenDurationTicksIsNull()
    {
        // Arrange
        var condition = new TimeWindowCondition();
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(5),
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
    public void EvaluateReturnsTrueWhenDurationWithinWindow()
    {
        // Arrange
        var condition = new TimeWindowCondition();
        var windowDuration = TimeSpan.FromMinutes(10);
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(5),
            DurationTicks = windowDuration.Ticks
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
    public void EvaluateReturnsFalseWhenDurationExceedsWindow()
    {
        // Arrange
        var condition = new TimeWindowCondition();
        var windowDuration = TimeSpan.FromMinutes(5);
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(10),
            DurationTicks = windowDuration.Ticks
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
    public void EvaluateReturnsTrueWhenDurationEqualsWindow()
    {
        // Arrange
        var condition = new TimeWindowCondition();
        var windowDuration = TimeSpan.FromMinutes(5);
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(5),
            DurationTicks = windowDuration.Ticks
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
    public void EvaluateRespectsNegation()
    {
        // Arrange
        var condition = new TimeWindowCondition();
        var windowDuration = TimeSpan.FromMinutes(10);
        var context = new NotificationContext
        {
            Duration = TimeSpan.FromMinutes(5),
            DurationTicks = windowDuration.Ticks,
            IsNegated = true
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse(); // Negated: 5 <= 10 is true, but negated becomes false
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConditionHasCorrectMetadata()
    {
        // Arrange & Act
        var condition = new TimeWindowCondition();

        // Assert
        condition.Id.ShouldBe(6);
        condition.Name.ShouldBe("TimeWindow");
        condition.Icon.ShouldBe("schedule");
        condition.Color.ShouldBe("Info");
    }
}
