namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Summary statistics for nullability analysis.
/// </summary>
public sealed class NullabilitySummary
{
    /// <summary>
    /// Gets or sets the total count of symbols.
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// Gets or sets the count of nullable symbols.
    /// </summary>
    public required int Nullable { get; init; }

    /// <summary>
    /// Gets or sets the count of non-nullable symbols.
    /// </summary>
    public required int NonNullable { get; init; }
}