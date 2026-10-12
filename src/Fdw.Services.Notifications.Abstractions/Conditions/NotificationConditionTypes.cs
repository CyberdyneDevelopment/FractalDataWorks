using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Collection of notification condition types with evaluation behavior.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(NotificationConditionTypeBase), typeof(INotificationConditionType), typeof(NotificationConditionTypes))]
public abstract partial class NotificationConditionTypes : TypeCollectionBase<NotificationConditionTypeBase, INotificationConditionType>
{
}
