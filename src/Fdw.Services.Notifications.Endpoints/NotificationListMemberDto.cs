using System;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// DTO for a member of a notification list.
/// </summary>
public sealed class NotificationListMemberDto
{
    /// <summary>Gets or sets the member unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the member name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the recipient address.</summary>
    public required string Recipient { get; set; }

    /// <summary>Gets or sets the recipient type (User, Email, Webhook, SlackChannel).</summary>
    public required string RecipientType { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets whether the member is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }
}
