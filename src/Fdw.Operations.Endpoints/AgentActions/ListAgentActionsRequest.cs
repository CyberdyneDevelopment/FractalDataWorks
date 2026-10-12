using FastEndpoints;

namespace Fdw.Operations.Endpoints.AgentActions;

/// <summary>
/// Request for listing agent actions with an optional status filter.
/// </summary>
public class ListAgentActionsRequest
{
    /// <summary>Gets or sets the optional status filter (Pending, Approved, Denied).</summary>
    [QueryParam]
    public string? Status { get; set; }
}
