using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Information about an impacted DataSet.
/// </summary>
public sealed class ImpactedDataSetPayload
{
    /// <summary>Gets or sets the DataSet name.</summary>
    public string DataSetName { get; set; } = string.Empty;
    /// <summary>Gets or sets the DataSet category.</summary>
    public string? Category { get; set; }
    /// <summary>Gets or sets the impact level.</summary>
    public string ImpactLevel { get; set; } = string.Empty;
    /// <summary>Gets or sets count of affected sources.</summary>
    public int AffectedSourceCount { get; set; }
    /// <summary>Gets or sets names of affected sources.</summary>
    public IReadOnlyList<string> AffectedSources { get; set; } = Array.Empty<string>();
}
