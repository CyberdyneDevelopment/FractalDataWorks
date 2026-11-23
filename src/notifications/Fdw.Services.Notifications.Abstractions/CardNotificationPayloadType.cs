using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>A card shown to a person: a title, a body and, optionally, what the person can do.</summary>
[TypeOption(typeof(NotificationPayloadTypes), "Card")]
[ExcludeFromCodeCoverage]
public sealed class CardNotificationPayloadType : NotificationPayloadTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CardNotificationPayloadType"/> class.</summary>
    public CardNotificationPayloadType() : base("Card", "A card shown to a person: a title, a body and, optionally, what the person can do.", [new PayloadField("title", true), new PayloadField("body", true), new PayloadField("actions", false)])
    {
    }
}
