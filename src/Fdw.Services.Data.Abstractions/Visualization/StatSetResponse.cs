using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Response containing computed statistical summaries keyed by column name.
/// </summary>
public sealed class StatSetResponse
{
    /// <summary>Gets or sets the statistics per column.</summary>
    public IReadOnlyDictionary<string, ColumnStatSet> ColumnStats { get; set; }
        = new Dictionary<string, ColumnStatSet>(System.StringComparer.Ordinal);
}
