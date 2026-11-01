using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Agents.Abstractions;

/// <summary>
/// Data record for the <c>agent.AgentAction</c> table.
/// Represents a queued mutating request from an AI agent pending human review.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AgentActionRecord
{
    /// <summary>Gets or sets the agent action identifier.</summary>
    public int RowId { get; set; }

    /// <summary>Gets or sets the logical identity, minted by the application before insert.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the agent key identifier.</summary>
    public int AgentKeyId { get; set; }

    /// <summary>Gets or sets the human-readable agent label.</summary>
    public string AgentLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the user ID the agent acts on behalf of.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the HTTP route of the intercepted request.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Gets or sets the HTTP method of the intercepted request.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON-serialized request body.</summary>
    public string? RequestBody { get; set; }

    /// <summary>Gets or sets the review status (Pending, Approved, Denied).</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Gets or sets when the action was requested.</summary>
    public DateTimeOffset RequestedAt { get; set; }

    /// <summary>Gets or sets when the action was reviewed.</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>Gets or sets who reviewed the action.</summary>
    public string? ReviewedBy { get; set; }
}
