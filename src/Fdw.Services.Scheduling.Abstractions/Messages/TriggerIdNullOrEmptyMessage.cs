using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the trigger ID is null or empty.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("TriggerIdNullOrEmpty")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class TriggerIdNullOrEmptyMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerIdNullOrEmptyMessage"/> class.
    /// </summary>
    public TriggerIdNullOrEmptyMessage()
        : base(1001, "TriggerIdNullOrEmpty", MessageSeverity.Error,
               "Trigger ID cannot be null or empty", "SCHED_TRIGGER_ID_NULL")
    { }
}
