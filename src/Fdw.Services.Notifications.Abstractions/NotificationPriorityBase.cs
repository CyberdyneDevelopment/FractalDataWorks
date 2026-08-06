using Fdw.Collections;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Base class for notification priority levels.
/// </summary>
public abstract class NotificationPriorityBase : TypeOptionBase<int, NotificationPriorityBase>, INotificationPriority
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotificationPriorityBase"/>.
    /// </summary>
    protected NotificationPriorityBase(int id, string name) : base(id, name) { }
}
