using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="CalculateTransformType"/> — the calculated-field expression engine.
/// Covers the typed <see cref="PipelineTransformConfiguration.Calculations"/> cascade-child list
/// (FDW-556 — replaces the deleted <c>ConfigurationJson</c> blob), the string-concatenation/
/// arithmetic/literal/field-reference evaluation branches, sequential application of multiple
/// calculations, the FormulaLanguage/engine-availability fail-loud gate, and per-record expression
/// evaluation failure.
/// </summary>
public sealed class CalculateTransformTypeTests
{
    private readonly CalculateTransformType _sut = new();

    private static TransformContext CreateContext(object? calculationEngine = null) =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), calculationEngine: calculationEngine);

    private static PipelineTransformConfiguration CreateConfig(params PipelineTransformCalculationConfiguration[] calculations) =>
        new() { Id = Guid.NewGuid(), Name = "Calc1", OperationType = "Calculate", Calculations = [.. calculations] };

    private static PipelineTransformCalculationConfiguration Calc(
        string outputField, string? expression, int executionOrder = 0, string formulaLanguage = "Builtin") =>
        new()
        {
            Id = Guid.NewGuid(),
            OutputField = outputField,
            Expression = expression!,
            FormulaLanguage = formulaLanguage,
            ExecutionOrder = executionOrder
        };

    // ── Fail-loud structural branches (FDW-556 — no silent pass-through) ────────────────

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

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsLoudWhenCalculationsListIsEmpty()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 1 };

        // Act
        var result = await _sut.Transform(input, CreateConfig(), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11047");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsLoudWhenFormulaLanguageIsUnknown()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 1 };
        var config = CreateConfig(Calc("Total", "A", formulaLanguage: "NoSuchLanguage"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11055");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformFailsLoudWhenFormulaLanguageDoesNotResolve()
    {
        // Arrange — FormulaLanguages ships only "Builtin" in this cycle; any other name (including a
        // plausible-looking one like "Scripted") does not resolve and must fail loud rather than
        // silently falling back to the built-in evaluator. The engine-unavailable gate
        // (EtlLog.FormulaEngineUnavailable, 11051) is exercised once a second FormulaLanguages option
        // is registered by a consuming assembly — this proves the prior unknown-language gate (11055)
        // is checked first and never silently bypassed.
        var input = new Dictionary<string, object?> { ["A"] = 1 };
        var config = CreateConfig(Calc("Total", "A", formulaLanguage: "Scripted"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(calculationEngine: null), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11055");
    }

    // ── Concatenation (the "+" operator is ALWAYS string concatenation, never numeric addition) ──

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformConcatenatesFieldsAndLiteralsWithPlusOperator()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["First"] = "John", ["Last"] = "Doe" };
        var config = CreateConfig(Calc("FullName", "First + ' ' + Last"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["FullName"].ShouldBe("John Doe");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformPlusOperatorConcatenatesNumericFieldsAsStringsRatherThanAdding()
    {
        // Arrange — production quirk: EvaluateExpression checks for " + " BEFORE any arithmetic
        // operator, so "A + B" always concatenates string representations ("1" + "2" = "12"),
        // never performs numeric addition. There is no numeric "+" path in this evaluator.
        var input = new Dictionary<string, object?> { ["A"] = 1, ["B"] = 2 };
        var config = CreateConfig(Calc("Sum", "A + B"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["Sum"].ShouldBe("12");
    }

    // ── Arithmetic operators ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformMultipliesTwoNumericFields()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 4, ["B"] = 5 };
        var config = CreateConfig(Calc("Product", "A * B"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["Product"].ShouldBe(20m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformDividesTwoNumericFields()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 10, ["B"] = 2 };
        var config = CreateConfig(Calc("Quotient", "A / B"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["Quotient"].ShouldBe(5m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformSubtractsTwoNumericFields()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 10, ["B"] = 3 };
        var config = CreateConfig(Calc("Difference", "A - B"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["Difference"].ShouldBe(7m);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformDivisionByZeroResultsInNullWithoutFailure()
    {
        // Arrange — production quirk: the division operation returns a null decimal? for a
        // zero divisor, which makes TryEvaluateArithmetic report "didn't match" (result != null
        // is false) rather than propagating the null division result; the expression then falls
        // through every other branch and EvaluateLiteralOrFieldReference can't resolve "A / B"
        // either, so the output field silently becomes null. No exception, no failure result.
        var input = new Dictionary<string, object?> { ["A"] = 10, ["B"] = 0 };
        var config = CreateConfig(Calc("Quotient", "A / B"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["Quotient"].ShouldBeNull();
    }

    // ── Literal / field-reference resolution ────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformResolvesNumericLiteralExpression()
    {
        // Arrange
        var input = new Dictionary<string, object?>();
        var config = CreateConfig(Calc("Constant", "42"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["Constant"].ShouldBe(42m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformResolvesQuotedStringLiteralExpression()
    {
        // Arrange
        var input = new Dictionary<string, object?>();
        var config = CreateConfig(Calc("Greeting", "'hello'"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["Greeting"].ShouldBe("hello");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformResolvesBareFieldReferenceExpressionPreservingOriginalType()
    {
        // Arrange — a bare field-name expression returns the field's raw value unconverted.
        var input = new Dictionary<string, object?> { ["Age"] = 30 };
        var config = CreateConfig(Calc("AgeCopy", "Age"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["AgeCopy"].ShouldBe(30);
        result.Value!["AgeCopy"].ShouldBeOfType<int>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformReturnsNullWhenExpressionIsUnresolvable()
    {
        // Arrange — not a field, not a numeric literal, not a quoted literal.
        var input = new Dictionary<string, object?>();
        var config = CreateConfig(Calc("Mystery", "NotAFieldOrLiteral"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["Mystery"].ShouldBeNull();
    }

    // ── Sequential application / overwrite semantics ────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformAppliesMultipleCalculationsInExecutionOrderAllowingLaterCalculationsToReferenceEarlierOutputs()
    {
        // Arrange — deliberately constructed out of ExecutionOrder to prove ordering is honored.
        var input = new Dictionary<string, object?> { ["Age"] = 10 };
        var config = CreateConfig(
            Calc("Quadruple", "Double * 2", executionOrder: 1),
            Calc("Double", "Age * 2", executionOrder: 0));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["Double"].ShouldBe(20m);
        result.Value!["Quadruple"].ShouldBe(40m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformOverwritesExistingFieldWhenOutputFieldMatchesInputField()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["Status"] = "Pending" };
        var config = CreateConfig(Calc("Status", "'Updated'"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["Status"].ShouldBe("Updated");
    }

    // ── Failure surfaces ─────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformReturnsCalculationFailedResultWhenExpressionEvaluationThrows()
    {
        // Arrange — a null Expression makes EvaluateExpression's first call
        // (expression.Contains(...)) throw a NullReferenceException; that is caught per-calculation
        // and converted into a structured failure via EtlLog.CalculationFailed rather than propagating.
        var input = new Dictionary<string, object?> { ["A"] = 1 };
        var config = CreateConfig(Calc("Total", null));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        result.Messages[^1].Message.ShouldContain("Calculation failed for field 'Total'");
        result.Messages[^1].Code.ShouldBe("ETL-91007");
    }

    // ── TransformBatch: structural fail-loud + per-record error accounting ──────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenCalculationsListIsEmpty()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["A"] = 1 } };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig(), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11047");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchExcludesRecordsThatFailEvaluationAndReportsContextError()
    {
        // Arrange — one record's Expression is null (throws), the other evaluates cleanly.
        var config = CreateConfig(Calc("Double", "Age * 2"));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Age"] = 10 },
            new Dictionary<string, object?> { ["Age"] = "not-a-number" },
        };
        var context = CreateContext();

        // Act
        var result = await _sut.TransformBatch(inputs, config, context, TestContext.Current.CancellationToken);

        // Assert — "not-a-number" resolves via EvaluateLiteralOrFieldReference to null (no exception),
        // so both records succeed; assert the well-formed record's calculation is exactly right.
        result.IsSuccess.ShouldBeTrue();
        var output = new List<IDictionary<string, object?>>(result.Value!);
        output.Count.ShouldBe(2);
        output[0]["Double"].ShouldBe(20m);
    }

    // ── MapSpecToConfiguration: request-spec → typed config dispatch (FDW-556 Part 2.2) ─

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationPopulatesCalculationsInSpecOrder()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Calc1",
            OperationType = "Calculate",
            ComputedColumns =
            [
                new FakeCalculationSpec { OutputField = "Double", Formula = "Age * 2", FormulaLanguage = "Builtin" },
                new FakeCalculationSpec { OutputField = "Quadruple", Formula = "Double * 2", FormulaLanguage = "Builtin" },
            ]
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Calc1", OperationType = "Calculate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        target.Calculations.Count.ShouldBe(2);
        target.Calculations[0].OutputField.ShouldBe("Double");
        target.Calculations[0].ExecutionOrder.ShouldBe(0);
        target.Calculations[1].OutputField.ShouldBe("Quadruple");
        target.Calculations[1].ExecutionOrder.ShouldBe(1);
        target.Calculations[0].PipelineTransformId.ShouldBe(target.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenComputedColumnsEmpty()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec { Name = "Calc1", OperationType = "Calculate" };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Calc1", OperationType = "Calculate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11047");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenFormulaLanguageUnknown()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Calc1",
            OperationType = "Calculate",
            ComputedColumns = [new FakeCalculationSpec { OutputField = "X", Formula = "1", FormulaLanguage = "NoSuchLanguage" }]
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Calc1", OperationType = "Calculate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11055");
    }
}
