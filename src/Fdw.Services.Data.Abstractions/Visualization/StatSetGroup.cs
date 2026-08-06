using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Represents a single group in grouped statistical results.
/// </summary>
public sealed class StatSetGroup
{
    /// <summary>Gets or sets the group key values keyed by column name.</summary>
    public IReadOnlyDictionary<string, object?> GroupKeys { get; set; }
        = new Dictionary<string, object?>(System.StringComparer.Ordinal);

    /// <summary>Gets or sets the statistics per column within this group.</summary>
    public IReadOnlyDictionary<string, ColumnStatSet> ColumnStats { get; set; }
        = new Dictionary<string, ColumnStatSet>(System.StringComparer.Ordinal);
}
