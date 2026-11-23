using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>A summary of what a run did: a title and the text.</summary>
[TypeOption(typeof(NotificationPayloadTypes), "Summary")]
[ExcludeFromCodeCoverage]
public sealed class SummaryNotificationPayloadType : NotificationPayloadTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SummaryNotificationPayloadType"/> class.</summary>
    public SummaryNotificationPayloadType() : base("Summary", "A summary of what a run did: a title and the text.", [new PayloadField("title", true), new PayloadField("text", true)])
    {
    }
}
