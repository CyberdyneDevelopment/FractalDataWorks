using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Notification failed to send.</summary>
[TypeOption(typeof(NotificationStatuses), "Failed")]
[ExcludeFromCodeCoverage]
public sealed class FailedNotificationStatus : NotificationStatusBase
{
    /// <summary>Initializes a new instance of <see cref="FailedNotificationStatus"/>.</summary>
    public FailedNotificationStatus() : base(3, "Failed") { }
}
