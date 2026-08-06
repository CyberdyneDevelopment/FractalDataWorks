using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Notification was rejected by the channel.</summary>
[TypeOption(typeof(NotificationStatuses), "Rejected")]
[ExcludeFromCodeCoverage]
public sealed class RejectedNotificationStatus : NotificationStatusBase
{
    /// <summary>Initializes a new instance of <see cref="RejectedNotificationStatus"/>.</summary>
    public RejectedNotificationStatus() : base(4, "Rejected") { }
}
