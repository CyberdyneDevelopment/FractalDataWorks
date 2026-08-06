using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Low priority - can be batched or delayed.</summary>
[TypeOption(typeof(NotificationPriorities), "Low")]
[ExcludeFromCodeCoverage]
public sealed class LowNotificationPriority : NotificationPriorityBase
{
    /// <summary>Initializes a new instance of <see cref="LowNotificationPriority"/>.</summary>
    public LowNotificationPriority() : base(0, "Low") { }
}
