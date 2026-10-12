using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>A calculation set compiled once and run any number of times over different rows.</summary>
public interface ICompiledCalculationSet
{
    /// <summary>Gets the names of the outputs the set writes, in order.</summary>
    IReadOnlyList<string> OutputNames { get; }

    /// <summary>Runs the set; the first row it cannot build fails it.</summary>
    /// <param name="scalars">The single values the calculation reads by name.</param>
    /// <param name="tables">The rows of each table input, by the table's name.</param>
    /// <returns>The tables written, or the first coded refusal.</returns>
    IGenericResult<CalculationSetResult> Evaluate(
        IReadOnlyDictionary<string, object?> scalars,
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> tables);

    /// <summary>Runs the set; the caller decides what becomes of each row it cannot build.</summary>
    /// <param name="scalars">The single values the calculation reads by name.</param>
    /// <param name="tables">The rows of each table input, by the table's name.</param>
    /// <param name="onRowFailure">Called once for each row that cannot be built.</param>
    /// <returns>The tables written, or the refusal of a row the handler did not drop, or of a failure that belongs to the whole set.</returns>
    IGenericResult<CalculationSetResult> Evaluate(
        IReadOnlyDictionary<string, object?> scalars,
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> tables,
        SetRowFailureHandler onRowFailure);
}
