using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the trigger name is null or empty.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("TriggerNameNullOrEmpty")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class TriggerNameNullOrEmptyMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerNameNullOrEmptyMessage"/> class.
    /// </summary>
    public TriggerNameNullOrEmptyMessage()
        : base(1002, "TriggerNameNullOrEmpty", MessageSeverity.Error,
               "Trigger name cannot be null or empty", "SCHED_TRIGGER_NAME_NULL")
    { }
}
