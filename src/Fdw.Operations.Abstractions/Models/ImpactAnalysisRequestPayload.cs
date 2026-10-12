namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Request for impact analysis.
/// </summary>
public sealed class ImpactAnalysisRequestPayload
{
    /// <summary>Gets or sets the target type to analyze (e.g., Connection, DataStore).</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>Gets or sets the target name to analyze.</summary>
    public string TargetName { get; set; } = string.Empty;
}
