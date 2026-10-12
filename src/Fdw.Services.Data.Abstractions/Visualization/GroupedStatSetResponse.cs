using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Response containing grouped statistical summaries.
/// </summary>
public sealed class GroupedStatSetResponse
{
    /// <summary>Gets or sets the grouped statistics. Each entry represents one group with its key values and column stats.</summary>
    public IReadOnlyList<StatSetGroup> Groups { get; set; } = [];
}
