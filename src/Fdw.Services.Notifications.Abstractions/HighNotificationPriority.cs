using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>High priority - sent immediately.</summary>
[TypeOption(typeof(NotificationPriorities), "High")]
[ExcludeFromCodeCoverage]
public sealed class HighNotificationPriority : NotificationPriorityBase
{
    /// <summary>Initializes a new instance of <see cref="HighNotificationPriority"/>.</summary>
    public HighNotificationPriority() : base(2, "High") { }
}
