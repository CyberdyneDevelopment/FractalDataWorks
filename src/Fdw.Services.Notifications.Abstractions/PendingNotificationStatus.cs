using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Notification is pending to be sent.</summary>
[TypeOption(typeof(NotificationStatuses), "Pending")]
[ExcludeFromCodeCoverage]
public sealed class PendingNotificationStatus : NotificationStatusBase
{
    /// <summary>Initializes a new instance of <see cref="PendingNotificationStatus"/>.</summary>
    public PendingNotificationStatus() : base(0, "Pending") { }
}
