using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>A reply to a person: the text of the answer.</summary>
[TypeOption(typeof(NotificationPayloadTypes), "Reply")]
[ExcludeFromCodeCoverage]
public sealed class ReplyNotificationPayloadType : NotificationPayloadTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ReplyNotificationPayloadType"/> class.</summary>
    public ReplyNotificationPayloadType() : base("Reply", "A reply to a person: the text of the answer.", [new PayloadField("text", true)])
    {
    }
}
