namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Request for impact analysis.
/// </summary>
// Why this lives here rather than being reused from Fdw.Operations.Endpoints: the clients package
// does not reference the endpoints package, and the response payload beside it is declared the same
// way. Property names match ImpactAnalysisRequest so the body binds on arrival.
public sealed class ImpactAnalysisRequestPayload
{
    /// <summary>Gets or sets the target type to analyze (e.g., Connection, DataStore).</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>Gets or sets the target name to analyze.</summary>
    public string TargetName { get; set; } = string.Empty;
}
