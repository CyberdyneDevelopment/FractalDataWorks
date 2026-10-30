using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Maps a stored notification to what the notification endpoints answer, so the read of one and the list of many say the same thing
/// about it: whether it is enabled and which secret it uses are stored on the domain row and every implementation declares them.
/// </summary>
public static class NotificationMapper
{
    /// <summary>Maps an implementation record, as read by name, to the detail the get answers.</summary>
    /// <param name="notification">The implementation record the provider read.</param>
    /// <returns>The detail, carrying the stored enabled flag and secret names.</returns>
    public static NotificationDetailDto ToDetail(INotificationImplementationConfiguration notification) => new()
    {
        Id = notification.Id,
        Name = notification.Name,
        Implementation = notification.Implementation,
        IsEnabled = notification.IsEnabled,
        SecretManagerName = notification.SecretManagerName,
        SecretKeyName = notification.SecretKeyName,
        Description = notification.Description
    };

    /// <summary>Maps a domain row, as read by the list, to the summary the list answers.</summary>
    /// <param name="item">The notification's domain row.</param>
    /// <returns>The summary, carrying the stored enabled flag.</returns>
    public static NotificationSummaryDto ToSummary(NotificationDomainConfiguration item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Implementation = item.Implementation,
        IsEnabled = item.IsEnabled,
        Description = item.Description
    };
}
