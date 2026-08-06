using System;

namespace Fdw.Services.Messaging.Clients.Models;

/// <summary>
/// Client-side payload for in-system messages.
/// Mirrors the API contract from Fdw.Services.Messaging.
/// </summary>
public sealed class MessagePayload
{
    /// <summary>Gets or sets the unique message identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the tenant that owns the message.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the identifier of the recipient user, if the message is directed to one.</summary>
    public Guid? RecipientUserId { get; set; }

    /// <summary>Gets or sets the identifier of the sending user, if the message has one.</summary>
    public Guid? SenderUserId { get; set; }

    /// <summary>Gets or sets the message type (category) discriminator.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity level of the message.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Gets or sets the message subject line.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gets or sets the message body, if any.</summary>
    public string? Body { get; set; }

    /// <summary>Gets or sets the type of the resource the message relates to, if any.</summary>
    public string? ResourceType { get; set; }

    /// <summary>Gets or sets the identifier of the resource the message relates to, if any.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Gets or sets an external correlation/reference identifier, if any.</summary>
    public string? ReferenceId { get; set; }

    /// <summary>Gets or sets the type of action the message invites, if any.</summary>
    public string? ActionType { get; set; }

    /// <summary>Gets or sets the URL for the message's action, if any.</summary>
    public string? ActionUrl { get; set; }

    /// <summary>Gets or sets the current status of the message.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets when the message was delivered, if applicable.</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>Gets or sets when the message was read, if applicable.</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>Gets or sets when the message was dismissed, if applicable.</summary>
    public DateTime? DismissedAt { get; set; }

    /// <summary>Gets or sets when the message was archived, if applicable.</summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>Gets or sets when the message was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets a value indicating whether the message is unread — neither read, dismissed, nor archived.</summary>
    public bool IsUnread => ReadAt is null && DismissedAt is null && ArchivedAt is null;
}
