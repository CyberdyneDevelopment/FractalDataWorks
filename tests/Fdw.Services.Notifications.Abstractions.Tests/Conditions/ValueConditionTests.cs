using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests.Conditions;

/// <summary>
/// Tests for ValueCondition evaluation behavior.
/// </summary>
public class ValueConditionTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateReturnsFalseWhenOperatorIsNull()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = null,
            Value = "test",
            ActualValue = "test"
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
    public void EvaluateReturnsFalseWhenOperatorIsEmpty()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = string.Empty,
            Value = "test",
            ActualValue = "test"
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
    public void EvaluateReturnsFalseWhenValueIsNull()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "Equal",
            Value = null,
            ActualValue = "test"
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
    public void EvaluateReturnsFalseWhenActualValueIsNull()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "Equal",
            Value = "test",
            ActualValue = null
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
    public void EvaluateEqualOperatorReturnsTrueWhenValuesMatch()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "Equal",
            Value = "test",
            ActualValue = "test"
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
    public void EvaluateEqualOperatorReturnsFalseWhenValuesDontMatch()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "Equal",
            Value = "test1",
            ActualValue = "test2"
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
    public void EvaluateNotEqualOperatorReturnsTrueWhenValuesDontMatch()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "NotEqual",
            Value = "test1",
            ActualValue = "test2"
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
    public void EvaluateContainsOperatorReturnsTrueWhenActualContainsValue()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "Contains",
            Value = "test",
            ActualValue = "this is a test value"
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
    public void EvaluateContainsOperatorReturnsFalseWhenActualDoesNotContainValue()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "Contains",
            Value = "missing",
            ActualValue = "this is a test value"
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
    public void EvaluateStartsWithOperatorReturnsTrueWhenActualStartsWithValue()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "StartsWith",
            Value = "test",
            ActualValue = "test value"
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
    public void EvaluateStartsWithOperatorReturnsFalseWhenActualDoesNotStartWithValue()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "StartsWith",
            Value = "prefix",
            ActualValue = "test value"
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
    public void EvaluateEndsWithOperatorReturnsTrueWhenActualEndsWithValue()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "EndsWith",
            Value = "value",
            ActualValue = "test value"
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
    public void EvaluateEndsWithOperatorReturnsFalseWhenActualDoesNotEndWithValue()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "EndsWith",
            Value = "suffix",
            ActualValue = "test value"
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
    public void EvaluateGreaterThanOperatorReturnsTrueForNumericComparison()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "GreaterThan",
            Value = "5",
            ActualValue = "10"
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
    public void EvaluateGreaterThanOperatorReturnsFalseForNumericComparison()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "GreaterThan",
            Value = "10",
            ActualValue = "5"
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
    public void EvaluateLessThanOperatorReturnsTrueForNumericComparison()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "LessThan",
            Value = "10",
            ActualValue = "5"
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
    public void EvaluateLessThanOperatorReturnsFalseForNumericComparison()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "LessThan",
            Value = "5",
            ActualValue = "10"
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
    public void EvaluateGreaterThanOrEqualOperatorReturnsTrueWhenEqual()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "GreaterThanOrEqual",
            Value = "10",
            ActualValue = "10"
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
    public void EvaluateGreaterThanOrEqualOperatorReturnsTrueWhenGreater()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "GreaterThanOrEqual",
            Value = "5",
            ActualValue = "10"
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
    public void EvaluateLessThanOrEqualOperatorReturnsTrueWhenEqual()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "LessThanOrEqual",
            Value = "10",
            ActualValue = "10"
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
    public void EvaluateLessThanOrEqualOperatorReturnsTrueWhenLess()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "LessThanOrEqual",
            Value = "10",
            ActualValue = "5"
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
    public void EvaluateUnknownOperatorReturnsFalse()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "UnknownOperator",
            Value = "test",
            ActualValue = "test"
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
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "Equal",
            Value = "test",
            ActualValue = "test",
            IsNegated = true
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse(); // Negated: equal is true, but negated becomes false
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EvaluateNumericComparisonFallsBackToStringComparisonForInvalidNumbers()
    {
        // Arrange
        var condition = new ValueCondition();
        var context = new NotificationContext
        {
            Operator = "GreaterThan",
            Value = "abc",
            ActualValue = "def"
        };

        // Act
        var result = condition.Evaluate(context);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue(); // "def" > "abc" in string comparison (d > a)
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConditionHasCorrectMetadata()
    {
        // Arrange & Act
        var condition = new ValueCondition();

        // Assert
        condition.Id.ShouldBe(5);
        condition.Name.ShouldBe("ValueCondition");
        condition.Icon.ShouldBe("compare");
        condition.Color.ShouldBe("Primary");
    }
}
