using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the trigger type is null or empty.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("TriggerTypeNullOrEmpty")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class TriggerTypeNullOrEmptyMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerTypeNullOrEmptyMessage"/> class.
    /// </summary>
    public TriggerTypeNullOrEmptyMessage()
        : base(1003, "TriggerTypeNullOrEmpty", MessageSeverity.Error,
               "Trigger type cannot be null or empty", "SCHED_TRIGGER_TYPE_NULL")
    { }
}
