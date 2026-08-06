using System;
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
/// Covers the per-step derivation <see cref="CalculationStepExecutor"/> emits — the record that
/// lets a produced figure (a bill line, a settlement amount) be traced back through every
/// intermediate to the inputs it came from.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public class CalculationStepLineageTests
{
    private static CalculationStepExecutor CreateExecutor() =>
        new(NullLogger<CalculationStepExecutor>.Instance);

    private static CalculationStepOperandConfiguration Operand(
        string name, string operandType, string? inputAlias = null, string? stepAlias = null,
        string? literalValue = null, string? fieldName = null) =>
        new()
        {
            Name = name,
            OperandType = operandType,
            InputAlias = inputAlias,
            StepAlias = stepAlias,
            LiteralValue = literalValue,
            FieldName = fieldName,
        };

    private static CalculationStepConfiguration Step(
        string name, string operationType, string outputAlias, int ordinal,
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

    private static decimal Number(CalculationTraceValue value) =>
        decimal.Parse(value.Text!, CultureInfo.InvariantCulture);

    /// <summary>
    /// The canonical billing shape: a determinant from an input, allocated proportionally, then
    /// adjusted — and every intermediate has to be explainable afterwards.
    /// </summary>
    [Fact]
    public async Task TraceRecordsEveryStepInOrderWithItsOperationAndOutput()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("allocate", "ProportionalAllocation", "allocated", ordinal: 1,
                Operand("Part", "Input", inputAlias: "usage", fieldName: "kwh"),
                Operand("Whole", "Literal", literalValue: "1000"),
                Operand("Total", "Literal", literalValue: "500")),
            Step("adjust", "Add", "final", ordinal: 2,
                Operand("Left", "StepReference", stepAlias: "allocated"),
                Operand("Right", "Literal", literalValue: "10")),
        };

        var recorder = new CalculationTraceRecorder();
        var result = await CreateExecutor().Execute(
            steps,
            [Input("usage", new Dictionary<string, object?> { ["kwh"] = 250m })],
            recorder,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        // (250/1000)*500 = 125, then +10 = 135
        decimal.Parse(result.Value!.ToString()!, CultureInfo.InvariantCulture).ShouldBe(135m);

        recorder.Steps.Count.ShouldBe(2);
        recorder.Steps[0].Ordinal.ShouldBe(1);
        recorder.Steps[0].StepName.ShouldBe("allocate");
        recorder.Steps[0].OperationType.ShouldBe("ProportionalAllocation");
        recorder.Steps[0].OutputAlias.ShouldBe("allocated");
        recorder.Steps[0].Completed.ShouldBeTrue();
        Number(recorder.Steps[0].OutputValue!).ShouldBe(125m);

        recorder.Steps[1].StepName.ShouldBe("adjust");
        recorder.Steps[1].Completed.ShouldBeTrue();
        Number(recorder.Steps[1].OutputValue!).ShouldBe(135m);
    }

    [Fact]
    public async Task TraceRecordsEachOperandSourceKindAndReference()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("allocate", "ProportionalAllocation", "allocated", ordinal: 1,
                Operand("Part", "Input", inputAlias: "usage", fieldName: "kwh"),
                Operand("Whole", "Literal", literalValue: "1000"),
                Operand("Total", "Literal", literalValue: "500")),
            Step("adjust", "Add", "final", ordinal: 2,
                Operand("Left", "StepReference", stepAlias: "allocated"),
                Operand("Right", "Literal", literalValue: "10")),
        };

        var recorder = new CalculationTraceRecorder();
        await CreateExecutor().Execute(
            steps,
            [Input("usage", new Dictionary<string, object?> { ["kwh"] = 250m })],
            recorder,
            TestContext.Current.CancellationToken);

        var first = recorder.Steps[0];

        // Input operand: names its alias, the field it narrowed to, and the value that produced.
        first.Operands[0].OperandName.ShouldBe("Part");
        first.Operands[0].SourceKind.ShouldBe("Input");
        first.Operands[0].SourceReference.ShouldBe("usage");
        first.Operands[0].FieldName.ShouldBe("kwh");
        Number(first.Operands[0].ResolvedValue).ShouldBe(250m);

        // Literal operand: the configured text is the source reference.
        first.Operands[1].SourceKind.ShouldBe("Literal");
        first.Operands[1].SourceReference.ShouldBe("1000");
        first.Operands[1].FieldName.ShouldBeNull();

        // Step reference: names the earlier alias AND carries the value that alias held.
        var second = recorder.Steps[1];
        second.Operands[0].SourceKind.ShouldBe("StepReference");
        second.Operands[0].SourceReference.ShouldBe("allocated");
        Number(second.Operands[0].ResolvedValue).ShouldBe(125m);
    }

    /// <summary>
    /// The chain has to be walkable: each StepReference operand must name an alias some earlier
    /// step published, which is what makes the trace a derivation rather than a list of events.
    /// </summary>
    [Fact]
    public async Task TraceFormsAWalkableChainFromInputsToFinalValue()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("a", "Add", "sum", ordinal: 1,
                Operand("Left", "Input", inputAlias: "x"),
                Operand("Right", "Literal", literalValue: "5")),
            Step("b", "Multiply", "scaled", ordinal: 2,
                Operand("Left", "StepReference", stepAlias: "sum"),
                Operand("Right", "Literal", literalValue: "2")),
            Step("c", "Negate", "final", ordinal: 3,
                Operand("Value", "StepReference", stepAlias: "scaled")),
        };

        var recorder = new CalculationTraceRecorder();
        var result = await CreateExecutor().Execute(
            steps, [Input("x", 10m)], recorder, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        // (10+5)*2 = 30, negated = -30
        decimal.Parse(result.Value!.ToString()!, CultureInfo.InvariantCulture).ShouldBe(-30m);

        var publishedByEarlierSteps = new HashSet<string>();
        foreach (var step in recorder.Steps)
        {
            foreach (var operand in step.Operands)
            {
                if (string.Equals(operand.SourceKind, "StepReference", System.StringComparison.Ordinal))
                {
                    publishedByEarlierSteps.ShouldContain(operand.SourceReference);
                }
            }

            publishedByEarlierSteps.Add(step.OutputAlias);
        }

        // The returned value is the last step's output — a reader never has to correlate by hand.
        Number(recorder.Steps[^1].OutputValue!).ShouldBe(-30m);
    }

    [Fact]
    public async Task TraceIsPopulatedForASingleStepCalculation()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("only", "PassThrough", "out", ordinal: 1,
                Operand("Value", "Input", inputAlias: "raw")),
        };

        var recorder = new CalculationTraceRecorder();
        var result = await CreateExecutor().Execute(
            steps, [Input("raw", 7m)], recorder, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        recorder.Steps.Count.ShouldBe(1);
        recorder.Steps[0].OperationType.ShouldBe("PassThrough");
        result.Value.ShouldBe(7m);
    }

    /// <summary>
    /// The case the trace exists for: a calculation that stops partway still has to account for
    /// the steps it did complete and name the one it stopped on.
    /// </summary>
    [Fact]
    public async Task TraceRetainsCompletedStepsWhenALaterStepFails()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("a", "Add", "sum", ordinal: 1,
                Operand("Left", "Input", inputAlias: "x"),
                Operand("Right", "Literal", literalValue: "5")),
            Step("b", "Multiply", "scaled", ordinal: 2,
                Operand("Left", "StepReference", stepAlias: "sum"),
                Operand("Right", "Input", inputAlias: "missing")),
            Step("c", "Negate", "final", ordinal: 3,
                Operand("Value", "StepReference", stepAlias: "scaled")),
        };

        var recorder = new CalculationTraceRecorder();
        var result = await CreateExecutor().Execute(
            steps, [Input("x", 10m)], recorder, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();

        // A failed result does not merely hold a null value — reading it throws. This is the
        // invariant the recorder exists for: a trace returned in the result value would be
        // unreachable on exactly the path where it matters most.
        Should.Throw<InvalidOperationException>(() => result.Value);

        // The completed step survives the failure, in full.
        recorder.Steps.Count.ShouldBe(2);
        recorder.Steps[0].StepName.ShouldBe("a");
        recorder.Steps[0].Completed.ShouldBeTrue();
        Number(recorder.Steps[0].OutputValue!).ShouldBe(15m);

        // The failing step is present, marked incomplete, and says why.
        recorder.Steps[1].StepName.ShouldBe("b");
        recorder.Steps[1].Ordinal.ShouldBe(2);
        recorder.Steps[1].OperationType.ShouldBe("Multiply");
        recorder.Steps[1].Completed.ShouldBeFalse();
        recorder.Steps[1].OutputValue.ShouldBeNull();
        recorder.Steps[1].Failure.ShouldNotBeNull();

        // Operands bound before the failure are kept, which is what locates the operand it stopped
        // on: it is the one after the last recorded entry.
        recorder.Steps[1].Operands.Count.ShouldBe(1);
        recorder.Steps[1].Operands[0].OperandName.ShouldBe("Left");
        Number(recorder.Steps[1].Operands[0].ResolvedValue).ShouldBe(15m);
    }

    /// <summary>
    /// A step rejected before it runs still has to appear, or the trace would end at the last
    /// successful step and say nothing about why execution stopped there.
    /// </summary>
    [Fact]
    public async Task TraceRecordsTheStepRejectedForADuplicateOutputAlias()
    {
        var steps = new List<IGenericConfiguration>
        {
            Step("a", "Add", "sum", ordinal: 1,
                Operand("Left", "Input", inputAlias: "x"),
                Operand("Right", "Literal", literalValue: "5")),
            Step("b", "Add", "sum", ordinal: 2,
                Operand("Left", "Input", inputAlias: "x"),
                Operand("Right", "Literal", literalValue: "1")),
        };

        var recorder = new CalculationTraceRecorder();
        var result = await CreateExecutor().Execute(
            steps, [Input("x", 10m)], recorder, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        recorder.Steps.Count.ShouldBe(2);
        recorder.Steps[1].StepName.ShouldBe("b");
        recorder.Steps[1].Completed.ShouldBeFalse();
        recorder.Steps[1].Failure.ShouldNotBeNull();

        // Rejected before binding, so it bound nothing — the trace says that rather than implying
        // the step got partway in.
        recorder.Steps[1].Operands.ShouldBeEmpty();
    }

    /// <summary>
    /// A step list the executor cannot even order produces no step trace, because no step ran.
    /// </summary>
    [Fact]
    public async Task TraceIsEmptyWhenNoStepsAreConfigured()
    {
        var recorder = new CalculationTraceRecorder();
        var result = await CreateExecutor().Execute(
            [], [Input("x", 10m)], recorder, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        recorder.Steps.ShouldBeEmpty();
    }
}
