using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>The tables a calculation set wrote, by output name. Each row maps a column name to its value.</summary>
public sealed class CalculationSetResult
{
    /// <summary>Initializes a new instance of the <see cref="CalculationSetResult"/> class.</summary>
    /// <param name="tables">The rows of each output.</param>
    public CalculationSetResult(IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> tables)
    {
        Tables = tables;
    }

    /// <summary>Gets the rows of each output, by the output's name.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> Tables { get; }
}
