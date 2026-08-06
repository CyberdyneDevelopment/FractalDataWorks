using System;

namespace Fdw.Agents.Clients.Models;

/// <summary>
/// Data transfer object for an agent action review item.
/// </summary>
public sealed class AgentActionPayload
{
    /// <summary>Gets or sets the unique action identifier.</summary>
    public int AgentActionId { get; set; }

    /// <summary>Gets or sets the agent key identifier.</summary>
    public int AgentKeyId { get; set; }

    /// <summary>Gets or sets the human-readable label of the agent.</summary>
    public string AgentLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the user identifier associated with the agent.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the API route the agent requested.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Gets or sets the HTTP method the agent used (GET, POST, PUT, DELETE, PATCH).</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized JSON request body, if any.</summary>
    public string? RequestBody { get; set; }

    /// <summary>Gets or sets the current review status (Pending, Approved, Denied).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets when the action was requested.</summary>
    public DateTimeOffset RequestedAt { get; set; }

    /// <summary>Gets or sets when the action was reviewed, if applicable.</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>Gets or sets the user ID of the reviewer, if applicable.</summary>
    public string? ReviewedBy { get; set; }
}
