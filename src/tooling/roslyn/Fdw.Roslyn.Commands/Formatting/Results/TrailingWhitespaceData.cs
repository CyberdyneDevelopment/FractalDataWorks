using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Formatting.Results;

/// <summary>
/// Data for trailing whitespace removal.
/// </summary>
public sealed class TrailingWhitespaceData
{
    /// <summary>
    /// Gets or sets the number of lines with trailing whitespace.
    /// </summary>
    public int LineCount { get; set; }

    /// <summary>
    /// Gets or sets the list of affected line numbers (1-based).
    /// </summary>
    public IReadOnlyList<int> AffectedLines { get; set; } = System.Array.Empty<int>();
}
