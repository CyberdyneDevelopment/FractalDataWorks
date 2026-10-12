using Fdw.Collections;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Interface for notification priority levels.
/// </summary>
public interface INotificationPriority : ITypeOption<int, NotificationPriorityBase> { }
