using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Runs a compiled single-value formula (a condition or a row count) over one record.</summary>
public static class SetValue
{
    /// <summary>Evaluates the formula and returns its one result.</summary>
    /// <param name="formula">The compiled formula; its first result is the value.</param>
    /// <param name="record">The record the formula reads.</param>
    /// <returns>The number, or the engine's coded failure.</returns>
    public static IGenericResult<decimal> Of(ICompiledCalculation formula, IReadOnlyDictionary<string, object?> record)
    {
        var results = new decimal[formula.EmittedNames.Count];
        var state = new CalculationRunState();
        return formula.TryEvaluate(record, results, state)
            ? GenericResult<decimal>.Success(results[0])
            : formula.Describe(state).ToNewResult<decimal>();
    }
}
