using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="FilterTransformType"/> — the per-row predicate engine. Covers the
/// IExpressionEvaluator seam, the built-in fallback expression parser (logical/null/string/
/// comparison operators), and both the single-record <c>Transform</c> and batch
/// <c>TransformBatch</c> entry points.
/// </summary>
public sealed class FilterTransformTypeTests
{
    private readonly FilterTransformType _sut = new();

    private static TransformContext CreateContext(object? calculationEngine = null) =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), calculationEngine: calculationEngine);

    private static PipelineTransformConfiguration CreateConfig(string? filterExpression) =>
        new() { Id = Guid.NewGuid(), Name = "Filter1", OperationType = "Filter", FilterExpression = filterExpression };

    // ── Transform: fail-loud guard branches (FDW-556 — no silent pass-through) ─────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsLoudWhenFilterExpressionIsNull()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 1 };

        // Act
        var result = await _sut.Transform(input, CreateConfig(null), CreateContext(), TestContext.Current.CancellationToken);

        // Assert — a param-less combine op fails loud, it never silently passes every record through
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11048");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsLoudWhenFilterExpressionIsWhitespace()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 1 };

        // Act
        var result = await _sut.Transform(input, CreateConfig("   "), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11048");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsLoudWhenConfigurationIsNotPipelineTransformConfiguration()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 1 };
        var configuration = Mock.Of<IGenericConfiguration>();

        // Act
        var result = await _sut.Transform(input, configuration, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11052");
    }

    // ── Transform: predicate pass / fail ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformReturnsInputWhenRecordPassesPredicate()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["Age"] = 20 };

        // Act
        var result = await _sut.Transform(input, CreateConfig("Age >= 18"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(input);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformReturnsNullValueWhenRecordFailsPredicate()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["Age"] = 10 };

        // Act
        var result = await _sut.Transform(input, CreateConfig("Age >= 18"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert — filtered-out records are a SUCCESS result carrying a null value, not a failure
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ── Transform: IExpressionEvaluator seam ────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformUsesCalculationEngineWhenItSucceeds()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["Age"] = 5 };
        var evaluator = new Mock<IExpressionEvaluator>();
        evaluator
            .Setup(e => e.EvaluatePredicate("garbage expression", It.IsAny<IReadOnlyDictionary<string, object?>>()))
            .Returns(GenericResult<bool>.Success(true));

        // Act — the built-in parser would never understand "garbage expression"; only the engine can pass it
        var result = await _sut.Transform(input, CreateConfig("garbage expression"), CreateContext(evaluator.Object), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(input);
        evaluator.Verify(e => e.EvaluatePredicate("garbage expression", It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformFallsBackToBuiltInEvaluationWhenCalculationEngineFails()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["Age"] = 20 };
        var evaluator = new Mock<IExpressionEvaluator>();
        evaluator
            .Setup(e => e.EvaluatePredicate(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>()))
            .Returns(GenericResult<bool>.Failure(EvaluatorFailureMessage()));

        // Act
        var result = await _sut.Transform(input, CreateConfig("Age >= 18"), CreateContext(evaluator.Object), TestContext.Current.CancellationToken);

        // Assert — built-in fallback still evaluates the expression correctly
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(input);
    }

    private static Fdw.Messages.IGenericMessage EvaluatorFailureMessage() =>
        Fdw.Messages.GenericMessage.Create(Fdw.Messages.MessageSeverity.Error, "evaluator exploded", null, null);

    // ── TransformBatch: pass-through / empty / cancellation ─────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenNoFilterExpression()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["A"] = 1 },
            new Dictionary<string, object?> { ["A"] = 2 },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig(null), CreateContext(), TestContext.Current.CancellationToken);

        // Assert — a param-less Filter step must fail loud, never silently pass every record through
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11048");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchReturnsEmptyForEmptyInput()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>>();

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig("Age >= 18"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchKeepsOnlyRecordsThatPassThePredicateInOrder()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Age"] = 10 },
            new Dictionary<string, object?> { ["Age"] = 20 },
            new Dictionary<string, object?> { ["Age"] = 30 },
            new Dictionary<string, object?> { ["Age"] = 5 },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig("Age >= 18"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var kept = new List<IDictionary<string, object?>>(result.Value!);
        kept.Count.ShouldBe(2);
        kept[0]["Age"].ShouldBe(20);
        kept[1]["Age"].ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchThrowsWhenCancellationAlreadyRequested()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Age"] = 20 },
        };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => _sut.TransformBatch(inputs, CreateConfig("Age >= 18"), CreateContext(), cts.Token));
    }

    // ── Built-in expression evaluator: theory-driven coverage ──────────────────────────

    private static Dictionary<string, object?> BuiltInRecord() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "Alice",
        ["Age"] = 30,
        ["Active"] = true,
        ["Tag"] = null,
    };

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData("Age >= 18", true)]
    [InlineData("Age >= 31", false)]
    [InlineData("Age <= 30", true)]
    [InlineData("Age <= 29", false)]
    [InlineData("Age > 29", true)]
    [InlineData("Age > 30", false)]
    [InlineData("Age < 31", true)]
    [InlineData("Age < 30", false)]
    [InlineData("Age == 30", true)]
    [InlineData("Age != 30", false)]
    [InlineData("Name == 'Alice'", true)]
    [InlineData("Name != 'Alice'", false)]
    [InlineData("Name contains 'lic'", true)]
    [InlineData("Name CONTAINS 'zzz'", false)]
    [InlineData("Name startswith 'Al'", true)]
    [InlineData("Name endswith 'ce'", true)]
    [InlineData("Name endswith 'zz'", false)]
    [InlineData("Tag == null", true)]
    [InlineData("Tag != null", false)]
    [InlineData("Active", true)]
    [InlineData("!Active", false)]
    [InlineData("NOT Active", false)]
    [InlineData("(Age >= 18)", true)]
    [InlineData("Age >= 18 && Name == 'Alice'", true)]
    [InlineData("Age >= 99 || Name == 'Alice'", true)]
    [InlineData("Age >= 99 AND Name == 'Alice'", false)]
    [InlineData("Age >= 99 and Name == 'Alice'", false)]
    [InlineData("UnknownField", true)]
    [InlineData("MissingField == null", true)]
    [InlineData("MissingField != null", false)]
    [InlineData("Active == true", true)]
    public async Task TransformEvaluatesBuiltInExpression(string expression, bool expectedPass)
    {
        // Arrange
        var record = BuiltInRecord();

        // Act
        var result = await _sut.Transform(record, CreateConfig(expression), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        if (expectedPass)
        {
            result.Value.ShouldBeSameAs(record);
        }
        else
        {
            result.Value.ShouldBeNull();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBooleanFieldParsesStringTrueFalse()
    {
        // Arrange
        var trueRecord = new Dictionary<string, object?> { ["Flag"] = "true" };
        var falseRecord = new Dictionary<string, object?> { ["Flag"] = "false" };

        // Act
        var trueResult = await _sut.Transform(trueRecord, CreateConfig("Flag"), CreateContext(), TestContext.Current.CancellationToken);
        var falseResult = await _sut.Transform(falseRecord, CreateConfig("Flag"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        trueResult.Value.ShouldBeSameAs(trueRecord);
        falseResult.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBooleanFieldCheckOnlyTestsNullnessNotZeroValue()
    {
        // Arrange — a present-but-zero numeric field is still "truthy" because the built-in
        // evaluator only checks for null, not falsy/zero (a real quirk of the current parser).
        var record = new Dictionary<string, object?> { ["Count"] = 0 };

        // Act
        var result = await _sut.Transform(record, CreateConfig("Count"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeSameAs(record);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformComparisonFallsBackToStringWhenValueIsNotNumeric()
    {
        // Arrange
        var record = new Dictionary<string, object?> { ["Name"] = "Alice" };

        // Act — "Alice" > "Aaron" alphabetically (case-insensitive)
        var result = await _sut.Transform(record, CreateConfig("Name > 'Aaron'"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeSameAs(record);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData(typeof(double))]
    [InlineData(typeof(float))]
    [InlineData(typeof(long))]
    [InlineData(typeof(short))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(decimal))]
    public async Task TransformComparesNumericFieldsAcrossUnderlyingTypes(Type numericType)
    {
        // Arrange
        object value = numericType == typeof(double) ? 25.0
            : numericType == typeof(float) ? 25.0f
            : numericType == typeof(long) ? 25L
            : numericType == typeof(short) ? (short)25
            : numericType == typeof(byte) ? (byte)25
            : 25.0m;
        var record = new Dictionary<string, object?> { ["Value"] = value };

        // Act
        var result = await _sut.Transform(record, CreateConfig("Value >= 20"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeSameAs(record);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformStringOperatorReturnsFalseWhenFieldMissing()
    {
        // Arrange
        var record = new Dictionary<string, object?>();

        // Act
        var result = await _sut.Transform(record, CreateConfig("Name contains 'a'"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformEqualityTreatsNullActualAndEmptyExpectedAsEqual()
    {
        // Arrange
        var record = new Dictionary<string, object?> { ["Tag"] = null };

        // Act
        var result = await _sut.Transform(record, CreateConfig("Tag == ''"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeSameAs(record);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformEqualityReturnsFalseWhenFieldEntirelyMissing()
    {
        // Arrange
        var record = new Dictionary<string, object?>();

        // Act
        var result = await _sut.Transform(record, CreateConfig("Missing == 'x'"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformEvaluatesTopLevelOrAroundParenthesizedAndClause()
    {
        var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Age"] = 10,
            ["Active"] = false,
            ["Name"] = "Bob",
        };

        // Act
        var result = await _sut.Transform(record, CreateConfig("(Age>=18 && Active) || Name==Bob"), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(record);
    }

    // ── MapSpecToConfiguration: request-spec → typed config dispatch (FDW-556 Part 2.2) ─

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationPopulatesFilterExpression()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec { Name = "Filter1", OperationType = "Filter", FilterExpression = "Age >= 18" };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Filter1", OperationType = "Filter" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        target.FilterExpression.ShouldBe("Age >= 18");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenFilterExpressionMissing()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec { Name = "Filter1", OperationType = "Filter", FilterExpression = null };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Filter1", OperationType = "Filter" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11048");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenTargetIsWrongType()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec { Name = "Filter1", OperationType = "Filter", FilterExpression = "Age >= 18" };
        var target = Mock.Of<IGenericConfiguration>();

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11052");
    }
}
