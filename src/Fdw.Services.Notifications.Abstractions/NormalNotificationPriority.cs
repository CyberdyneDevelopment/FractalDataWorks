using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>Normal priority - sent at regular intervals.</summary>
[TypeOption(typeof(NotificationPriorities), "Normal")]
[ExcludeFromCodeCoverage]
public sealed class NormalNotificationPriority : NotificationPriorityBase
{
    /// <summary>Initializes a new instance of <see cref="NormalNotificationPriority"/>.</summary>
    public NormalNotificationPriority() : base(1, "Normal") { }
}
