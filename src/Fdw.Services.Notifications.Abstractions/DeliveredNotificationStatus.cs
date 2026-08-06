using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Notification delivery was confirmed by the channel.</summary>
[TypeOption(typeof(NotificationStatuses), "Delivered")]
[ExcludeFromCodeCoverage]
public sealed class DeliveredNotificationStatus : NotificationStatusBase
{
    /// <summary>Initializes a new instance of <see cref="DeliveredNotificationStatus"/>.</summary>
    public DeliveredNotificationStatus() : base(2, "Delivered") { }
}
