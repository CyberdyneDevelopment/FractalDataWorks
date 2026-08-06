using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Response for impact analysis.
/// </summary>
public sealed class ImpactAnalysisPayload
{
    /// <summary>Gets or sets the type of target analyzed.</summary>
    public string TargetType { get; set; } = string.Empty;
    /// <summary>Gets or sets the name of target analyzed.</summary>
    public string TargetName { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of impacted DataSets.</summary>
    public IReadOnlyList<ImpactedDataSetPayload> ImpactedDataSets { get; set; } = Array.Empty<ImpactedDataSetPayload>();
    /// <summary>Gets or sets total count of impacted objects.</summary>
    public int TotalImpactedCount { get; set; }
    /// <summary>Gets or sets count of high impact objects.</summary>
    public int HighImpactCount { get; set; }
    /// <summary>Gets or sets analysis timestamp.</summary>
    public DateTime AnalyzedAt { get; set; }
}
