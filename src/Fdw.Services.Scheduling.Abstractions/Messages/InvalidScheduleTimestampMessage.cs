using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// Error message indicating that a schedule's updated timestamp is earlier than its created timestamp.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("InvalidScheduleTimestamp")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class InvalidScheduleTimestampMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidScheduleTimestampMessage"/> class.
    /// </summary>
    public InvalidScheduleTimestampMessage()
        : base(2007, "InvalidScheduleTimestamp", MessageSeverity.Error,
               "Updated timestamp cannot be earlier than created timestamp", "SCHED_INVALID_SCHEDULE_TIMESTAMP")
    { }
}
