using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Compilation;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// Shared helpers for the calculation tree tests: stated limits, building and compiling an array, and evaluating a record.
/// </summary>
internal static class CalculationTestSupport
{
    public static readonly CalculationLimits Limits = new(256, 5000, 64);

    public static CompiledCalculation Compile(CalculationArrayBuilder builder)
    {
        var built = builder.Build(Limits);
        built.IsSuccess.ShouldBeTrue();
        var compiled = new CalculationCompiler().Compile(built.Value!, Limits);
        compiled.IsSuccess.ShouldBeTrue();
        return compiled.Value!;
    }

    public static decimal[] Evaluate(CompiledCalculation compiled, Dictionary<string, object?> record)
    {
        var results = new decimal[compiled.EmittedNames.Count];
        var outcome = compiled.Evaluate(record, results);
        outcome.IsSuccess.ShouldBeTrue();
        return results;
    }

    public static string FailureOf(CompiledCalculation compiled, Dictionary<string, object?> record)
    {
        var outcome = compiled.Evaluate(record, new decimal[compiled.EmittedNames.Count]);
        outcome.IsFailure.ShouldBeTrue();
        outcome.Code.ShouldNotBeNull();
        return outcome.Code.Name;
    }

    public static string RefusalOf(IGenericResult result)
    {
        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldNotBeNull();
        return result.Code.Name;
    }

    public static Dictionary<string, object?> Record(params (string Field, object? Value)[] fields)
    {
        var record = new Dictionary<string, object?>(System.StringComparer.Ordinal);
        foreach (var (field, value) in fields)
        {
            record[field] = value;
        }

        return record;
    }
}
