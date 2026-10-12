using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>
/// A calculation array compiled to one delegate, as the code that runs it sees it: evaluate a record, and name what failed.
/// </summary>
/// <remarks>
/// Why an interface here: a pipeline context carries the calculations its run compiled, and the context lives in an
/// abstractions project that cannot reference the compiler. An instance is immutable and safe to share between threads; a
/// <see cref="CalculationRunState"/> is not, so each thread keeps its own.
/// </remarks>
public interface ICompiledCalculation
{
    /// <summary>Gets the names of the results the run writes out, in the order they are written to the results array.</summary>
    IReadOnlyList<string> EmittedNames { get; }

    /// <summary>Evaluates one record.</summary>
    /// <param name="record">The record's fields.</param>
    /// <param name="results">The array the emitted results are written to; it must hold at least <see cref="EmittedNames"/> entries.</param>
    /// <param name="state">The run state; it is reset first, and holds the first failure afterwards.</param>
    /// <returns>True when every result was computed; false when the state holds a failure and the results must not be used.</returns>
    bool TryEvaluate(IReadOnlyDictionary<string, object?> record, decimal[] results, CalculationRunState state);

    /// <summary>Turns the failure a state holds into a coded failed result.</summary>
    /// <param name="state">A state that holds a failure.</param>
    /// <returns>The failed result.</returns>
    IGenericResult Describe(CalculationRunState state);
}
