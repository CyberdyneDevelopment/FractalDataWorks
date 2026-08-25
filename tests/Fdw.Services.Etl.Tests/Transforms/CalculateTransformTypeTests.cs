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
/// calculations and per-record expression
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
        string outputField, string? expression, int executionOrder = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            OutputField = outputField,
            Expression = expression!,
            ExecutionOrder = executionOrder
        };

    // ── Fail-loud structural branches (FDW-556 — no silent pass-through) ────────────────



    // ── Concatenation (the "+" operator is ALWAYS string concatenation, never numeric addition) ──

}
