using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// TypeCollection for notification statuses.
/// </summary>
[TypeCollection(typeof(NotificationStatusBase), typeof(INotificationStatus), typeof(NotificationStatuses))]
[ExcludeFromCodeCoverage]
public abstract partial class NotificationStatuses : TypeCollectionBase<NotificationStatusBase, INotificationStatus> { }
