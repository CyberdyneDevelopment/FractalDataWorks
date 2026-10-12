using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// TypeCollection for notification priority levels.
/// </summary>
[TypeCollection(typeof(NotificationPriorityBase), typeof(INotificationPriority), typeof(NotificationPriorities))]
[ExcludeFromCodeCoverage]
public abstract partial class NotificationPriorities : TypeCollectionBase<NotificationPriorityBase, INotificationPriority> { }
