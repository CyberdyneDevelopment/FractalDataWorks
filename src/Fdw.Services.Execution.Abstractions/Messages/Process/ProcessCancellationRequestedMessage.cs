using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Process;

/// <summary>
/// CurrentMessage indicating that a process cancellation was requested.
/// </summary>
[Message("ProcessCancellationRequested")]
public sealed class ProcessCancellationRequestedMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessCancellationRequestedMessage"/> class.
    /// </summary>
    public ProcessCancellationRequestedMessage()
        : base(1003, "ProcessCancellationRequested", MessageSeverity.Information,
               "Process cancellation requested",
               "EXEC_PROCESS_CANCELLATION_REQUESTED",
               "Process",
               "https://docs.Fdw.io/execution/cancellation")
    { }

    /// <summary>
    /// Initializes a new instance with process ID.
    /// </summary>
    /// <param name="processId">The ID of the process for which cancellation was requested.</param>
    public ProcessCancellationRequestedMessage(string processId)
        : base(1003, "ProcessCancellationRequested", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Process cancellation requested for process: {0}", processId),
               "EXEC_PROCESS_CANCELLATION_REQUESTED",
               "Process",
               "https://docs.Fdw.io/execution/cancellation")
    { }

    /// <summary>
    /// Initializes a new instance with process ID and reason.
    /// </summary>
    /// <param name="processId">The ID of the process for which cancellation was requested.</param>
    /// <param name="reason">The reason for the cancellation request.</param>
    public ProcessCancellationRequestedMessage(string processId, string reason)
        : base(1003, "ProcessCancellationRequested", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Process cancellation requested for process: {0}, reason: {1}",
                           processId, reason),
               "EXEC_PROCESS_CANCELLATION_REQUESTED",
               "Process",
               "https://docs.Fdw.io/execution/cancellation")
    { }

    /// <summary>
    /// Initializes a new instance with process ID, operation name, and reason.
    /// </summary>
    /// <param name="processId">The ID of the process for which cancellation was requested.</param>
    /// <param name="operationName">The name of the operation being cancelled.</param>
    /// <param name="reason">The reason for the cancellation request.</param>
    public ProcessCancellationRequestedMessage(string processId, string operationName, string reason)
        : base(1003, "ProcessCancellationRequested", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Process cancellation requested for process: {0}, operation: {1}, reason: {2}",
                           processId, operationName, reason),
               "EXEC_PROCESS_CANCELLATION_REQUESTED",
               "Process",
               "https://docs.Fdw.io/execution/cancellation")
    { }
}