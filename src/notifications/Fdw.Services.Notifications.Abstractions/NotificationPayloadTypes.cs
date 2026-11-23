using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// The types of payload a pipeline may send through a notification rule (FDW-6138): Reply, Card and Summary. A package that knows another type adds its own option; a service that does not know a
/// type is sent the words.
/// </summary>
[TypeCollection(typeof(NotificationPayloadTypeBase), typeof(INotificationPayloadType), typeof(NotificationPayloadTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class NotificationPayloadTypes : TypeCollectionBase<NotificationPayloadTypeBase, INotificationPayloadType>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
