using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>
/// Data record for <c>notify.NotificationList</c>.
/// A notification list is a named group of recipients that can be referenced by notification rules.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class NotificationListConfiguration
{

    /// <summary>Gets or sets the durable logical identity of this notification list.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this notification list.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description of this notification list.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this notification list is enabled.</summary>
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
