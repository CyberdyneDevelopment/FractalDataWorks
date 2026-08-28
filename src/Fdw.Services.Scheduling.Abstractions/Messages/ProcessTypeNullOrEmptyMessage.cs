using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// Error message indicating that a process type is null or empty.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ProcessTypeNullOrEmpty")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class ProcessTypeNullOrEmptyMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessTypeNullOrEmptyMessage"/> class.
    /// </summary>
    public ProcessTypeNullOrEmptyMessage()
        : base(2004, "ProcessTypeNullOrEmpty", MessageSeverity.Error,
               "Process type cannot be null or empty", "SCHED_PROCESS_TYPE_NULL")
    { }
}
