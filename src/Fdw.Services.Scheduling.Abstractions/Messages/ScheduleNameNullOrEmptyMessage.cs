using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// Error message indicating that a schedule name is null or empty.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ScheduleNameNullOrEmpty")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class ScheduleNameNullOrEmptyMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleNameNullOrEmptyMessage"/> class.
    /// </summary>
    public ScheduleNameNullOrEmptyMessage()
        : base(2002, "ScheduleNameNullOrEmpty", MessageSeverity.Error,
               "Schedule name cannot be null or empty", "SCHED_SCHEDULE_NAME_NULL")
    { }
}
