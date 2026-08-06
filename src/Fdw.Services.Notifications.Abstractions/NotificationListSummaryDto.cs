using System;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Summary DTO for a notification list, used in list views.
/// </summary>
public sealed class NotificationListSummaryDto
{
    /// <summary>Gets or sets the list unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the list name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the list is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the number of members in this list.</summary>
    public int MemberCount { get; set; }
}
