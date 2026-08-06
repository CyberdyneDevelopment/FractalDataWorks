using System;
using System.Reflection;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Request for impact analysis.
/// </summary>
public class ImpactAnalysisRequest
{
    /// <summary>Gets or sets the target type to analyze (e.g., connection, datastore).</summary>
    public string TargetType { get; set; } = string.Empty;
    /// <summary>Gets or sets the target name to analyze.</summary>
    public string TargetName { get; set; } = string.Empty;
}