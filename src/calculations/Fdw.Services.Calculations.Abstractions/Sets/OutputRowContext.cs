using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>What an output column reads when one output row is built: the row's visible values and the results the formulas just computed.</summary>
public sealed class OutputRowContext
{
    private readonly string _output;

    /// <summary>Initializes a new instance of the <see cref="OutputRowContext"/> class.</summary>
    /// <param name="output">The output being built, for a refusal.</param>
    /// <param name="record">The values visible to this row, derived values included.</param>
    /// <param name="computed">The formula results, by column name.</param>
    public OutputRowContext(string output, IReadOnlyDictionary<string, object?> record, IReadOnlyDictionary<string, decimal> computed)
    {
        _output = output;
        Record = record;
        Computed = computed;
    }

    /// <summary>Gets the values visible to this row.</summary>
    public IReadOnlyDictionary<string, object?> Record { get; }

    /// <summary>Gets the formula results, by column name.</summary>
    public IReadOnlyDictionary<string, decimal> Computed { get; }

    /// <summary>Gets the first refusal a column hit while the row was built, or null.</summary>
    public IGenericResult? Failure { get; private set; }

    /// <summary>Reads a visible value; a missing one is recorded as a refusal and reads as null.</summary>
    /// <param name="column">The value's name.</param>
    /// <returns>The value.</returns>
    public object? Read(string column)
    {
        if (Record.TryGetValue(column, out var value))
        {
            return value;
        }

        Failure ??= SetFailure.Of("SetColumnMissing", _output, $"the row has no value '{column}' to copy");
        return null;
    }
}
