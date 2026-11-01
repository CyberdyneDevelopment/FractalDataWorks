using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// Error message indicating that a process configuration is null.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ProcessConfigurationNull")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class ProcessConfigurationNullMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationNullMessage"/> class.
    /// </summary>
    public ProcessConfigurationNullMessage()
        : base(2005, "ProcessConfigurationNull", MessageSeverity.Error,
               "Process configuration cannot be null", "SCHED_PROCESS_CONFIG_NULL")
    { }
}
