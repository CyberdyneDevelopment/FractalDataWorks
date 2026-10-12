namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Summary statistics for code smells.
/// </summary>
public sealed class CodeSmellsSummary
{
    /// <summary>
    /// Gets or sets the total count of smells.
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// Gets or sets the count of high severity smells.
    /// </summary>
    public required int High { get; init; }

    /// <summary>
    /// Gets or sets the count of medium severity smells.
    /// </summary>
    public required int Medium { get; init; }

    /// <summary>
    /// Gets or sets the count of low severity smells.
    /// </summary>
    public required int Low { get; init; }
}