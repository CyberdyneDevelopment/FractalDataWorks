using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// Error message indicating that a process ID is null or empty.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ProcessIdNullOrEmpty")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class ProcessIdNullOrEmptyMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessIdNullOrEmptyMessage"/> class.
    /// </summary>
    public ProcessIdNullOrEmptyMessage()
        : base(2003, "ProcessIdNullOrEmpty", MessageSeverity.Error,
               "Process ID cannot be null or empty", "SCHED_PROCESS_ID_NULL")
    { }
}
