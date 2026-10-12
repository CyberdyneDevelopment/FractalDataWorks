using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the trigger configuration is null.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("TriggerConfigurationNull")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class TriggerConfigurationNullMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerConfigurationNullMessage"/> class.
    /// </summary>
    public TriggerConfigurationNullMessage()
        : base(1004, "TriggerConfigurationNull", MessageSeverity.Error,
               "Trigger configuration cannot be null", "SCHED_CONFIG_NULL")
    { }
}
