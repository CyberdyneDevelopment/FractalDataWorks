using System;
using System.Reflection;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Response for impact analysis.
/// </summary>
public class ImpactAnalysisResponse
{
    /// <summary>Gets or sets the analyzed target type.</summary>
    public string TargetType { get; set; } = string.Empty;
    /// <summary>Gets or sets the analyzed target name.</summary>
    public string TargetName { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of impacted DataSets.</summary>
    public IList<ImpactedDataSetResponse> ImpactedDataSets { get; set; } = [];
    /// <summary>Gets or sets the total number of impacted DataSets.</summary>
    public int TotalImpactedCount { get; set; }
    /// <summary>Gets or sets the number of high-impact DataSets.</summary>
    public int HighImpactCount { get; set; }
    /// <summary>Gets or sets the timestamp of the analysis.</summary>
    public DateTime AnalyzedAt { get; set; }
}