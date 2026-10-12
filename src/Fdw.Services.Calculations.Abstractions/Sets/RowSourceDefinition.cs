using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>
/// Decides how many rows an output has and what its formulas can read for each: one per input row, a number worked out from
/// the data, or one per group.
/// </summary>
/// <remarks>
/// A source that needs formulas (a condition, a row count) lists them in <see cref="Formulas"/>; the compiler compiles them in
/// that order and hands the compiled forms back to <see cref="Drivers"/>. The source itself stays plain data and logic over rows.
/// </remarks>
public abstract class RowSourceDefinition
{
    private static readonly CalculationNode[] NoFormulas = [];
    private static readonly string[] NoNames = [];

    /// <summary>Gets the kind name this source is stored and chosen by: Each, Count or EachGroup.</summary>
    public abstract string Kind { get; }

    /// <summary>Gets the name the position 1 to N is readable under, or null when the source has no position.</summary>
    public virtual string? IndexColumn => null;

    /// <summary>Gets the columns whose values define a group; empty for a source that does not group.</summary>
    public virtual IReadOnlyList<string> GroupBy => NoNames;

    /// <summary>Gets the condition a row must meet as the author stated it, or null.</summary>
    public virtual CalculationNode? Condition => null;

    /// <summary>Gets the row count formula as the author stated it, or null.</summary>
    public virtual CalculationNode? CountFormula => null;

    /// <summary>Gets the formulas this source needs compiled, in the order <see cref="Drivers"/> receives them.</summary>
    public virtual IReadOnlyList<CalculationNode> Formulas => NoFormulas;

    /// <summary>Gets the table this source reads, or null when it reads none.</summary>
    public virtual string? TableName => null;

    /// <summary>Builds the rows an output is made for.</summary>
    /// <param name="output">The output asking, for a refusal.</param>
    /// <param name="scope">The values, tables and limits of this run.</param>
    /// <param name="compiled">The compiled <see cref="Formulas"/>, in order.</param>
    /// <returns>The driver rows, or a refusal.</returns>
    public abstract IGenericResult<IReadOnlyList<DriverRow>> Drivers(string output, SetRunScope scope, IReadOnlyList<ICompiledCalculation> compiled);

    /// <summary>Refuses a driver count over the row limit.</summary>
    /// <param name="output">The output asking.</param>
    /// <param name="count">How many rows are about to exist.</param>
    /// <param name="scope">The run, which holds the limit.</param>
    /// <returns>Success, or the refusal.</returns>
    protected static IGenericResult WithinLimit(string output, int count, SetRunScope scope) =>
        count <= scope.Limits.MaxRows
            ? GenericResult.Success()
            : SetFailure.Of("SetRowLimitExceeded", output, $"{count} rows; the limit is {scope.Limits.MaxRows}");
}
