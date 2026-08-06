using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>
/// Data record for <c>notify.NotificationListMember</c>.
/// A member of a notification list, representing a single recipient with a type and address.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class NotificationListMemberConfiguration
{

    /// <summary>Gets or sets the notification list this member belongs to.</summary>
    public Guid NotificationListId { get; set; }


    /// <summary>Gets or sets the durable logical identity of this member.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this member.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the recipient address (email, channel, URL, etc.).</summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of recipient (e.g., "Email", "SlackChannel", "Webhook").</summary>
    public string RecipientType { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name for this member.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets whether this member is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the ordinal position within the list.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether this is the current version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original source creation date (for imported data).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets when this version was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets who created this version.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets who this version was created on behalf of.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets when this version was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets who last modified this version.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets who this version was modified on behalf of.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
