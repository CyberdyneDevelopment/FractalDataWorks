using Fdw.Collections;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Base class for notification statuses.
/// </summary>
public abstract class NotificationStatusBase : TypeOptionBase<int, NotificationStatusBase>, INotificationStatus
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotificationStatusBase"/>.
    /// </summary>
    protected NotificationStatusBase(int id, string name) : base(id, name) { }
}
