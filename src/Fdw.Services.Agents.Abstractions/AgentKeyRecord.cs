using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Agents.Abstractions;

/// <summary>
/// Data record for the <c>cfg.AgentKey</c> table.
/// Represents an API key that grants AI agents access to WebMCP endpoints,
/// bound to a specific user identity for RBAC.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AgentKeyRecord
{
    /// <summary>Gets or sets the agent key identifier.</summary>
    public int AgentKeyId { get; set; }

    /// <summary>Gets or sets the user identity this agent acts on behalf of.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the user who owns this key.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable label for this key.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the PBKDF2 hash of the raw API key.</summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this key is currently active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the create date.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets who created this record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the last modified date.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets who last modified this record.</summary>
    public string ModifyBy { get; set; } = string.Empty;
}
