using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Tests for IExpressionEvaluator interface contract.
/// </summary>
public class IExpressionEvaluatorTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EvaluateCanBeCalledWithValidExpression()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator();
        var variables = new Dictionary<string, object?> { ["x"] = 5 };

        // Act
        var result = evaluator.Evaluate<int>("x * 2", variables);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EvaluateReturnsTypedResult()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator();
        var variables = new Dictionary<string, object?> { ["name"] = "Test" };

        // Act
        var result = evaluator.Evaluate<string>("name + ' Result'", variables);

        // Assert
        result.Value.ShouldBe("Test Result");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EvaluateHandlesEmptyVariables()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator();
        var variables = new Dictionary<string, object?>();

        // Act
        var result = evaluator.Evaluate<int>("42", variables);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EvaluatePredicateCanBeCalledWithBooleanExpression()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator();
        var variables = new Dictionary<string, object?> { ["age"] = 25 };

        // Act
        var result = evaluator.EvaluatePredicate("age > 18", variables);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EvaluatePredicateReturnsFalseForFalseCondition()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator();
        var variables = new Dictionary<string, object?> { ["age"] = 15 };

        // Act
        var result = evaluator.EvaluatePredicate("age > 18", variables);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateExpressionCanBeCalledWithValidExpression()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator();

        // Act
        var result = evaluator.ValidateExpression("x + y");

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateExpressionReturnsFailureForInvalidExpression()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator();

        // Act
        var result = evaluator.ValidateExpression("invalid expression !!!");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ValidateExpressionHandlesEmptyString()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator();

        // Act
        var result = evaluator.ValidateExpression(string.Empty);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EvaluateReturnsFailureWhenExpressionIsInvalid()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator(shouldFail: true);
        var variables = new Dictionary<string, object?>();

        // Act
        var result = evaluator.Evaluate<int>("invalid", variables);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EvaluatePredicateReturnsFailureWhenExpressionIsInvalid()
    {
        // Arrange
        var evaluator = new TestExpressionEvaluator(shouldFail: true);
        var variables = new Dictionary<string, object?>();

        // Act
        var result = evaluator.EvaluatePredicate("invalid", variables);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Test implementation of IExpressionEvaluator.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestExpressionEvaluator : IExpressionEvaluator
    {
        private readonly bool _shouldFail;

        public TestExpressionEvaluator(bool shouldFail = false)
        {
            _shouldFail = shouldFail;
        }

        public IGenericResult<T> Evaluate<T>(string expression, IReadOnlyDictionary<string, object?> variables)
        {
            if (_shouldFail)
            {
                return GenericResult<T>.Failure(new GenericMessage("Evaluation failed"));
            }

            // Simulate simple evaluation
            if (expression == "x * 2" && variables.TryGetValue("x", out var x))
            {
                return GenericResult<T>.Success((T)(object)((int)x! * 2));
            }
            if (expression == "name + ' Result'" && variables.TryGetValue("name", out var name))
            {
                return GenericResult<T>.Success((T)(object)$"{name} Result");
            }
            if (expression == "42")
            {
                return GenericResult<T>.Success((T)(object)42);
            }

            return GenericResult<T>.Success(default!);
        }

        public IGenericResult<bool> EvaluatePredicate(string expression, IReadOnlyDictionary<string, object?> variables)
        {
            if (_shouldFail)
            {
                return GenericResult<bool>.Failure(new GenericMessage("Evaluation failed"));
            }

            if (expression == "age > 18" && variables.TryGetValue("age", out var age))
            {
                return GenericResult<bool>.Success((int)age! > 18);
            }

            return GenericResult<bool>.Success(true);
        }

        public IGenericResult ValidateExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                return GenericResult.Failure(new GenericMessage("Expression is empty"));
            }

            if (expression.Contains("invalid"))
            {
                return GenericResult.Failure(new GenericMessage("Invalid expression syntax"));
            }

            return GenericResult.Success();
        }
    }
}
