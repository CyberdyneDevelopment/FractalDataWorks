using System.Collections.Generic;
using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// A notification type the application has, with the settings a notification of it needs (FDW-2931).
/// </summary>
public sealed class NotificationTypeDto
{
    /// <summary>Gets or sets the name of the type, which a notification is made with.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the name shown for the type.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets what the type does.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the settings a notification of the type needs beyond the ones every notification has.</summary>
    public IReadOnlyList<NotificationSettingField> Settings { get; set; } = [];
}
