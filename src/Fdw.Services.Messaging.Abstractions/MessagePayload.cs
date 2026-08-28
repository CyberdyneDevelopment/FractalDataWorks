using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// Data transfer object for in-system messages.
/// </summary>
[GenerateMapper]
public sealed class MessagePayload
{
    /// <summary>Gets or sets the message identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the recipient user identifier.</summary>
    public Guid? RecipientUserId { get; set; }

    /// <summary>Gets or sets the sender user identifier.</summary>
    public Guid? SenderUserId { get; set; }

    /// <summary>Gets or sets the message type.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity level.</summary>
    public string Severity { get; set; } = string.Empty;

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

    /// <summary>Gets or sets the message status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets when the message was delivered.</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>Gets or sets when the message was read.</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>Gets or sets when the message was dismissed.</summary>
    public DateTime? DismissedAt { get; set; }

    /// <summary>Gets or sets when the message was archived.</summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>Gets or sets when the message was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets a value indicating whether an agent sent this message rather than a person.
    /// </summary>
    /// <remarks>
    /// Derived from the stored message type, which the send endpoint sets from the caller's
    /// authentication scheme and refuses to take from the request body. It is exposed as its own
    /// field so a consumer has exactly one thing to trust for "who is speaking" instead of
    /// re-deriving identity from a type string, and so it keeps working unchanged when the
    /// underlying signal improves from the auth scheme to a real agent claim.
    ///
    /// Get-only on purpose: it is not a column, and the mappers only bind settable properties.
    /// </remarks>
    public bool IsAgent => string.Equals(MessageType, "AgentMessage", System.StringComparison.Ordinal);
}
