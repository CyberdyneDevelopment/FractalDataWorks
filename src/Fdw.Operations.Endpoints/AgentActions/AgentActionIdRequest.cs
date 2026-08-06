namespace Fdw.Operations.Endpoints.AgentActions;

/// <summary>
/// Request for getting, approving, or denying an agent action by ID.
/// </summary>
public class AgentActionIdRequest
{
    /// <summary>Gets or sets the agent action identifier.</summary>
    public int ActionId { get; set; }
}
