using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>
/// Data record for <c>notify.UserNotificationPreference</c>.
/// Stores per-user preferences for notification types and delivery channels.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class UserNotificationPreferenceConfiguration
{

    /// <summary>Gets or sets the user this preference belongs to.</summary>
    public Guid UserId { get; set; }


    /// <summary>Gets or sets the notification type (e.g., "PipelineFailure", "ScheduleMissed").</summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>Gets or sets the delivery channel (e.g., "Email", "Slack", "InApp").</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this preference is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

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
