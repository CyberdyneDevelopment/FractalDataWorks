using System;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// DTO for a notification recipient on a rule.
/// </summary>
public sealed class NotificationRecipientDto
{
    /// <summary>Gets or sets the recipient unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the recipient name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the recipient address.</summary>
    public required string Recipient { get; set; }

    /// <summary>Gets or sets the recipient type.</summary>
    public required string RecipientType { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets whether the recipient is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }
}
