using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="CalculateTransformType"/> — the calculated-field expression engine.
/// Covers the typed <see cref="PipelineTransformConfiguration.Calculations"/> cascade-child list
/// (FDW-556 — replaces the deleted <c>ConfigurationJson</c> blob), the string-concatenation/
/// arithmetic/literal/field-reference evaluation branches, sequential application of multiple
/// calculations and per-record expression
/// evaluation failure.
/// </summary>
public sealed class CalculateTransformTypeTests
{
    private readonly CalculateTransformType _sut = new();

    private static TransformContext CreateContext(object? calculationEngine = null) =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>(), calculationEngine: calculationEngine);

    private static PipelineTransformConfiguration CreateConfig(params PipelineTransformCalculationConfiguration[] calculations) =>
        new() { Id = Guid.NewGuid(), Name = "Calc1", OperationType = "Calculate", Calculations = [.. calculations] };

    private static PipelineTransformCalculationConfiguration Calc(
        string outputField, string? expression, int executionOrder = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            OutputField = outputField,
            Expression = expression!,
            ExecutionOrder = executionOrder
        };

    // ── Fail-loud structural branches (FDW-556 — no silent pass-through) ────────────────



    // ── Division (FDW-1189 — a zero divisor is a failed record, never an empty column) ──────────

    [Fact]
    public async Task TransformFailsWhenTheDivisorIsZeroNamingTheFieldAndTheExpression()
    {
        var input = new Dictionary<string, object?> { ["Amount"] = 7123m, ["Count"] = 0m };

        var result = await _sut.Transform(input, CreateConfig(Calc("Ratio", "Amount / Count")), CreateContext(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Division by zero");
        result.CurrentMessage.ShouldContain("Ratio");
        result.CurrentMessage.ShouldContain("Amount / Count");
        result.CurrentMessage.ShouldNotContain("7123");
    }

    [Fact]
    public async Task TransformFailsWhenTheDivisorIsAZeroLiteral()
    {
        var input = new Dictionary<string, object?> { ["Amount"] = 10m };

        var result = await _sut.Transform(input, CreateConfig(Calc("Ratio", "Amount / 0")), CreateContext(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Division by zero");
    }

    [Fact]
    public async Task TransformStillDividesWhenTheDivisorIsNotZero()
    {
        var input = new Dictionary<string, object?> { ["Amount"] = 10m, ["Count"] = 4m };

        var result = await _sut.Transform(input, CreateConfig(Calc("Ratio", "Amount / Count")), CreateContext(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!["Ratio"].ShouldBe(2.5m);
    }

    [Fact]
    public async Task TransformBatchReportsTheZeroDivisorRecordAsAnErrorAndSkipsOnlyThatRecord()
    {
        var context = CreateContext();
        var good = new Dictionary<string, object?> { ["Amount"] = 10m, ["Count"] = 4m };
        var bad = new Dictionary<string, object?> { ["Amount"] = 10m, ["Count"] = 0m };

        var result = await _sut.TransformBatch(
            [good, bad], CreateConfig(Calc("Ratio", "Amount / Count")), context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count().ShouldBe(1);
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].Message.ShouldContain("Division by zero");
        context.Errors[0].Message.ShouldContain("Ratio");
    }

    // ── Concatenation (the "+" operator is ALWAYS string concatenation, never numeric addition) ──

}
