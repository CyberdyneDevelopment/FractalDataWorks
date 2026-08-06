using System;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Information about an impacted DataSet.
/// </summary>
public class ImpactedDataSetResponse
{
    /// <summary>Gets or sets the impacted DataSet name.</summary>
    public string DataSetName { get; set; } = string.Empty;
    /// <summary>Gets or sets the DataSet category.</summary>
    public string? Category { get; set; }
    /// <summary>Gets or sets the impact level (High or Medium).</summary>
    public string ImpactLevel { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of affected sources within this DataSet.</summary>
    public int AffectedSourceCount { get; set; }
    /// <summary>Gets or sets the names of affected sources.</summary>
    public IList<string> AffectedSources { get; set; } = [];
}