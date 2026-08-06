using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests.Conditions;

/// <summary>
/// Tests for ExecutionStatusCondition evaluation behavior.
/// </summary>
public class ExecutionStatusConditionTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsFalseWhenValueIsNull()
    {
        // Arrange
        var condition = new ExecutionStatusCondition();
        var context = new NotificationContext
        {
            ExecutionStatus = "Failed",
            Value = null
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
    public void EvaluateReturnsFalseWhenValueIsEmpty()
    {
        // Arrange
        var condition = new ExecutionStatusCondition();
        var context = new NotificationContext
        {
            ExecutionStatus = "Failed",
            Value = string.Empty
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
    public void EvaluateReturnsFalseWhenExecutionStatusIsNull()
    {
        // Arrange
        var condition = new ExecutionStatusCondition();
        var context = new NotificationContext
        {
            ExecutionStatus = null,
            Value = "Failed"
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
    public void EvaluateReturnsFalseWhenExecutionStatusIsEmpty()
    {
        // Arrange
        var condition = new ExecutionStatusCondition();
        var context = new NotificationContext
        {
            ExecutionStatus = string.Empty,
            Value = "Failed"
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
    public void EvaluateReturnsTrueWhenStatusesMatch()
    {
        // Arrange
        var condition = new ExecutionStatusCondition();
        var context = new NotificationContext
        {
            ExecutionStatus = "Failed",
            Value = "Failed"
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
    public void EvaluateReturnsFalseWhenStatusesDontMatch()
    {
        // Arrange
        var condition = new ExecutionStatusCondition();
        var context = new NotificationContext
        {
            ExecutionStatus = "Failed",
            Value = "Succeeded"
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
    public void EvaluateIsCaseInsensitive()
    {
        // Arrange
        var condition = new ExecutionStatusCondition();
        var context = new NotificationContext
        {
            ExecutionStatus = "Failed",
            Value = "failed"
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
        var condition = new ExecutionStatusCondition();
        var context = new NotificationContext
        {
            ExecutionStatus = "Failed",
            Value = "Failed",
            IsNegated = true
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse(); // Negated: match is true, but negated becomes false
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConditionHasCorrectMetadata()
    {
        // Arrange & Act
        var condition = new ExecutionStatusCondition();

        // Assert
        condition.Id.ShouldBe(4);
        condition.Name.ShouldBe("ExecutionStatus");
        condition.Icon.ShouldBe("flag");
        condition.Color.ShouldBe("Info");
    }
}
