using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// Error message indicating that a trigger is null.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("TriggerNull")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class TriggerNullMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerNullMessage"/> class.
    /// </summary>
    public TriggerNullMessage()
        : base(2006, "TriggerNull", MessageSeverity.Error,
               "Trigger cannot be null", "SCHED_TRIGGER_NULL")
    { }
}
