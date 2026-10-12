using Fdw.Collections;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Base class for notification statuses.
/// </summary>
public abstract class NotificationStatusBase : TypeOptionBase<NotificationStatusBase>, INotificationStatus
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotificationStatusBase"/>.
    /// </summary>
    protected NotificationStatusBase(string name) : base(name) { }
}
