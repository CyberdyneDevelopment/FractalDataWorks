namespace Fdw.Roslyn.Commands.Formatting.Results;

/// <summary>
/// Data for line ending normalization.
/// </summary>
public sealed class LineEndingData
{
    /// <summary>
    /// Gets or sets the target line ending name.
    /// </summary>
    public string TargetLineEnding { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of line endings normalized.
    /// </summary>
    public int NormalizedCount { get; set; }

    /// <summary>
    /// Gets or sets the original CRLF count.
    /// </summary>
    public int OriginalCrlfCount { get; set; }

    /// <summary>
    /// Gets or sets the original LF count.
    /// </summary>
    public int OriginalLfCount { get; set; }

    /// <summary>
    /// Gets or sets the original CR count.
    /// </summary>
    public int OriginalCrCount { get; set; }
}
