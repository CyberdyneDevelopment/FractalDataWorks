using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;

namespace Fdw.Services.Calculations.Compilation;

/// <summary>
/// A calculation array compiled to one delegate. Evaluating a record reads each input field once, computes every result in
/// declared order with exact decimal arithmetic, and writes the emitted results to an array.
/// </summary>
/// <remarks>
/// An instance is immutable and safe to share between threads; a <see cref="CalculationRunState"/> is not, so each thread
/// keeps its own. A failure never throws: <see cref="TryEvaluate"/> returns false and the state names the first failure.
/// </remarks>
public sealed class CompiledCalculation : ICompiledCalculation
{
    private const string OverflowNode = "an arithmetic operation";

    private readonly Action<IReadOnlyDictionary<string, object?>, CalculationRunState, decimal[]> _evaluate;
    private readonly List<string> _resultNames;
    private readonly List<string> _nodeLabels;

    internal CompiledCalculation(
        Action<IReadOnlyDictionary<string, object?>, CalculationRunState, decimal[]> evaluate,
        List<string> emittedNames,
        List<string> resultNames,
        List<string> nodeLabels)
    {
        _evaluate = evaluate;
        EmittedNames = emittedNames;
        _resultNames = resultNames;
        _nodeLabels = nodeLabels;
    }

    /// <summary>Gets the names of the results the run writes out, in the order they are written to the results array.</summary>
    public IReadOnlyList<string> EmittedNames { get; }

    /// <summary>Evaluates one record.</summary>
    /// <param name="record">The record's fields.</param>
    /// <param name="results">The array the emitted results are written to; it must hold at least <see cref="EmittedNames"/> entries.</param>
    /// <param name="state">The run state; it is reset first, and holds the first failure afterwards.</param>
    /// <returns>True when every result was computed; false when the state holds a failure and the results must not be used.</returns>
    public bool TryEvaluate(IReadOnlyDictionary<string, object?> record, decimal[] results, CalculationRunState state)
    {
        state.Reset();
        _evaluate(record, state, results);
        return !state.Failed;
    }

    /// <summary>Evaluates one record and reports the outcome as a result.</summary>
    /// <param name="record">The record's fields.</param>
    /// <param name="results">The array the emitted results are written to.</param>
    /// <returns>Success, or the coded failure naming the result, the node and the reason.</returns>
    public IGenericResult Evaluate(IReadOnlyDictionary<string, object?> record, decimal[] results)
    {
        var state = new CalculationRunState();
        return TryEvaluate(record, results, state) ? GenericResult.Success() : Describe(state);
    }

    /// <summary>Turns the failure a state holds into a coded failed result.</summary>
    /// <param name="state">A state that holds a failure.</param>
    /// <returns>The failed result.</returns>
    public IGenericResult Describe(CalculationRunState state) =>
        GenericResult.Failure(
            CalculationTreeResultCodes.ById(state.Code),
            ResultDetails.Create(
                "result", _resultNames[state.FailedResult],
                "node", state.Node >= 0 && state.Node < _nodeLabels.Count ? _nodeLabels[state.Node] : OverflowNode,
                "reason", state.Reason));
}
