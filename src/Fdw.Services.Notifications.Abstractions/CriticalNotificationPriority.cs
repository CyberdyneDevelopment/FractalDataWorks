using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Critical priority - sent immediately with retry guarantees.</summary>
[TypeOption(typeof(NotificationPriorities), "Critical")]
[ExcludeFromCodeCoverage]
public sealed class CriticalNotificationPriority : NotificationPriorityBase
{
    /// <summary>Initializes a new instance of <see cref="CriticalNotificationPriority"/>.</summary>
    public CriticalNotificationPriority() : base(3, "Critical") { }
}
