using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Trees;

/// <summary>
/// A calculation array: an ordered list of named results computed from one record. A result may use only the results
/// before it.
/// </summary>
public sealed class CalculationArrayDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationArrayDefinition"/> class.
    /// </summary>
    /// <param name="name">The array name, for logs and refusals.</param>
    /// <param name="results">The results, in evaluation order.</param>
    public CalculationArrayDefinition(string name, IReadOnlyList<CalculationResultDefinition> results)
    {
        Name = name;
        Results = results;
    }

    /// <summary>Gets the array name.</summary>
    public string Name { get; }

    /// <summary>Gets the results, in evaluation order.</summary>
    public IReadOnlyList<CalculationResultDefinition> Results { get; }
}
