using System.Collections.Generic;

namespace Fdw.Commands.Data;

/// <summary>
/// Result from a find operation containing the matched record and which fields matched.
/// </summary>
/// <typeparam name="T">The type of the matched record.</typeparam>
public sealed class FindResult<T>
{
    /// <summary>
    /// Gets the matched record.
    /// </summary>
    public required T Record { get; init; }

    /// <summary>
    /// Gets the names of fields that matched the search term.
    /// </summary>
    public IReadOnlyList<string> MatchedFields { get; init; } = [];
}
