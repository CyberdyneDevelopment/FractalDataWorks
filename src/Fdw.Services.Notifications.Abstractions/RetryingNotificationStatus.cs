using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Notification is being retried.</summary>
[TypeOption(typeof(NotificationStatuses), "Retrying")]
[ExcludeFromCodeCoverage]
public sealed class RetryingNotificationStatus : NotificationStatusBase
{
    /// <summary>Initializes a new instance of <see cref="RetryingNotificationStatus"/>.</summary>
    public RetryingNotificationStatus() : base(5, "Retrying") { }
}
