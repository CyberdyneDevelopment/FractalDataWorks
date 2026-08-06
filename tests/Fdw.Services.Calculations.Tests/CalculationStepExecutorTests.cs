using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.Lineage;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Calculations.Lineage;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers <see cref="CalculationStepExecutor"/> — the loop that turns a configured entity's ordered
/// steps into a value by binding operands (input alias / prior step alias / literal) to the
/// registered calculation operations.
/// </summary>
/// <remarks>
/// The fail-loud cases carry as much weight as the happy path here: this executor backs billing-style
/// calculations, where a silently defaulted operand would produce a plausible wrong number rather
/// than a stop. Each negative test asserts a failure is returned, never a coerced value.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public class CalculationStepExecutorTests
{
    private static CalculationStepExecutor CreateExecutor() =>
        new(NullLogger<CalculationStepExecutor>.Instance);

    private static CalculationStepOperandConfiguration Operand(
        string name,
        string operandType,
        string? inputAlias = null,
        string? stepAlias = null,
        string? literalValue = null,
        string? fieldName = null,
        int ordinal = 0) =>
        new()
        {
            Name = name,
            OperandType = operandType,
            InputAlias = inputAlias,
            StepAlias = stepAlias,
            LiteralValue = literalValue,
            FieldName = fieldName,
            Ordinal = ordinal,
        };

    private static CalculationStepConfiguration Step(
        string name,
        string operationType,
        string outputAlias,
        int ordinal,
        params CalculationStepOperandConfiguration[] operands) =>
        new()
        {
            Name = name,
            OperationType = operationType,
            OutputAlias = outputAlias,
            Ordinal = ordinal,
            Operands = [.. operands],
        };

    private static ResolvedCalculationInput Input(string alias, object? value) =>
        new() { InputAlias = alias, ResolvedValue = value };

    [Fact]
    public async Task ExecuteRunsOrderedStepsAndReturnsFinalValue()
    {
        // base = 10 + 5 = 15, then 15 * 3 = 45 — the second step consumes the first via StepReference.
        var steps = new List<IGenericConfiguration>
        {
            Step("multiply", "Multiply", "product", ordinal: 2,
                Operand("Left", "StepReference", stepAlias: "sum"),
                Operand("Right", "Literal", literalValue: "3")),
            Step("add", "Add", "sum", ordinal: 1,
                Operand("Left", "Input", inputAlias: "base"),
                Operand("Right", "Literal", literalValue: "5")),
        };

        var result = await CreateExecutor().Execute(
            steps,
            [Input("base", 10m)],
            new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        decimal.Parse(result.Value!.ToString()!, CultureInfo.InvariantCulture).ShouldBe(45m);
    }

    [Fact]
    public async Task ExecuteResolvesOperandFieldFromRowInput()
    {
        var row = new Dictionary<string, object?> { ["amount"] = 7m };
        var steps = new List<IGenericConfiguration>
        {
            Step("add", "Add", "sum", ordinal: 1,
                Operand("Left", "Input", inputAlias: "row", fieldName: "amount"),
                Operand("Right", "Literal", literalValue: "1")),
        };

        var result = await CreateExecutor().Execute(
            steps,
            [Input("row", row)],
            new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        decimal.Parse(result.Value!.ToString()!, CultureInfo.InvariantCulture).ShouldBe(8m);
    }

    [Fact]
    public async Task ExecuteFailsWhenNoStepsConfigured()
    {
        var result = await CreateExecutor().Execute(
            [],
            [],
            new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFailsOnUnknownOperation()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("bogus", "NoSuchOperation", "out", ordinal: 1,
                Operand("Left", "Literal", literalValue: "1")),
        };

        var result = await CreateExecutor().Execute(steps, [], new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFailsOnUnresolvedInputAlias()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("add", "Add", "sum", ordinal: 1,
                Operand("Left", "Input", inputAlias: "missing"),
                Operand("Right", "Literal", literalValue: "1")),
        };

        // Why this must fail rather than treat the absent input as zero: a missing determinant is
        // the exact condition a billing calculation must stop on.
        var result = await CreateExecutor().Execute(steps, [], new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFailsOnForwardStepReference()
    {
        // Step 1 references an alias only step 2 publishes — a forward reference must not resolve.
        var steps = new List<IGenericConfiguration>
        {
            Step("first", "Add", "first", ordinal: 1,
                Operand("Left", "StepReference", stepAlias: "later"),
                Operand("Right", "Literal", literalValue: "1")),
            Step("second", "Add", "later", ordinal: 2,
                Operand("Left", "Literal", literalValue: "1"),
                Operand("Right", "Literal", literalValue: "1")),
        };

        var result = await CreateExecutor().Execute(steps, [], new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFailsOnUnknownOperandType()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("add", "Add", "sum", ordinal: 1,
                Operand("Left", "SomethingElse", literalValue: "1"),
                Operand("Right", "Literal", literalValue: "1")),
        };

        var result = await CreateExecutor().Execute(steps, [], new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFailsOnMissingLiteralValue()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("add", "Add", "sum", ordinal: 1,
                Operand("Left", "Literal", literalValue: null),
                Operand("Right", "Literal", literalValue: "1")),
        };

        var result = await CreateExecutor().Execute(steps, [], new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFailsWhenRequiredParameterHasNoOperand()
    {
        // Add declares Left and Right; only Left is bound.
        var steps = new List<IGenericConfiguration>
        {
            Step("add", "Add", "sum", ordinal: 1,
                Operand("Left", "Literal", literalValue: "1")),
        };

        var result = await CreateExecutor().Execute(steps, [], new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFailsOnDuplicateOutputAlias()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("one", "Add", "same", ordinal: 1,
                Operand("Left", "Literal", literalValue: "1"),
                Operand("Right", "Literal", literalValue: "1")),
            Step("two", "Add", "same", ordinal: 2,
                Operand("Left", "Literal", literalValue: "2"),
                Operand("Right", "Literal", literalValue: "2")),
        };

        var result = await CreateExecutor().Execute(steps, [], new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFailsWhenOperandNamesUnaddressableField()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("add", "Add", "sum", ordinal: 1,
                Operand("Left", "Input", inputAlias: "scalar", fieldName: "amount"),
                Operand("Right", "Literal", literalValue: "1")),
        };

        // The referenced value is a bare decimal — it exposes no "amount" field.
        var result = await CreateExecutor().Execute(
            steps,
            [Input("scalar", 10m)],
            new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecutePropagatesOperationFailureRatherThanDefaulting()
    {
        // Divide guards zero explicitly; the executor must surface that failure, not substitute 0.
        var steps = new List<IGenericConfiguration>
        {
            Step("divide", "Divide", "quotient", ordinal: 1,
                Operand("Left", "Literal", literalValue: "10"),
                Operand("Right", "Literal", literalValue: "0")),
        };

        var result = await CreateExecutor().Execute(steps, [], new CalculationTraceRecorder(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
