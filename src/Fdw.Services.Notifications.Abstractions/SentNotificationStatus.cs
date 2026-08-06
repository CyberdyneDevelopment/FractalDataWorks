using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Notification was sent successfully.</summary>
[TypeOption(typeof(NotificationStatuses), "Sent")]
[ExcludeFromCodeCoverage]
public sealed class SentNotificationStatus : NotificationStatusBase
{
    /// <summary>Initializes a new instance of <see cref="SentNotificationStatus"/>.</summary>
    public SentNotificationStatus() : base(1, "Sent") { }
}
