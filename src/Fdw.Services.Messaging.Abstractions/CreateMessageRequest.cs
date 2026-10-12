using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// Request to create a new in-system message.
/// </summary>
public sealed class CreateMessageRequest
{
    /// <summary>Gets or sets the tenant identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the recipient user identifier.</summary>
    public Guid? RecipientUserId { get; set; }

    /// <summary>Gets or sets the sender user identifier.</summary>
    public Guid? SenderUserId { get; set; }

    /// <summary>Gets or sets the message type.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity level. Defaults to Info.</summary>
    public string Severity { get; set; } = "Info";

    /// <summary>Gets or sets the message subject.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gets or sets the message body.</summary>
    public string? Body { get; set; }

    /// <summary>Gets or sets the resource type this message relates to.</summary>
    public string? ResourceType { get; set; }

    /// <summary>Gets or sets the resource identifier this message relates to.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Gets or sets a reference identifier for correlation.</summary>
    public string? ReferenceId { get; set; }

    /// <summary>Gets or sets the action type for actionable messages.</summary>
    public string? ActionType { get; set; }

    /// <summary>Gets or sets the action URL for actionable messages.</summary>
    public string? ActionUrl { get; set; }
}
