namespace Fdw.Services.Calculations.Abstractions.Trees;

/// <summary>
/// The stated size limits for a calculation array. There are no defaults: whoever builds or compiles an array states them.
/// </summary>
public sealed class CalculationLimits
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationLimits"/> class.
    /// </summary>
    /// <param name="maxResults">The most results one array may have.</param>
    /// <param name="maxNodes">The most nodes one array may have in all its trees together.</param>
    /// <param name="maxDepth">The deepest nesting one tree may have.</param>
    public CalculationLimits(int maxResults, int maxNodes, int maxDepth)
    {
        MaxResults = maxResults;
        MaxNodes = maxNodes;
        MaxDepth = maxDepth;
    }

    /// <summary>Gets the most results one array may have.</summary>
    public int MaxResults { get; }

    /// <summary>Gets the most nodes one array may have in all its trees together.</summary>
    public int MaxNodes { get; }

    /// <summary>Gets the deepest nesting one tree may have.</summary>
    public int MaxDepth { get; }
}
