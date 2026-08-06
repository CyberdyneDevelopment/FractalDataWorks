using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Process;

/// <summary>
/// CurrentMessage indicating that a process execution failed.
/// </summary>
[Message("ProcessExecutionFailed")]
public sealed class ProcessExecutionFailedMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExecutionFailedMessage"/> class.
    /// </summary>
    public ProcessExecutionFailedMessage()
        : base(1001, "ProcessExecutionFailed", MessageSeverity.Error,
               "Process execution failed",
               "EXEC_PROCESS_FAILED",
               "Process",
               "https://docs.Fdw.io/execution/process-management")
    { }

    /// <summary>
    /// Initializes a new instance with process ID.
    /// </summary>
    /// <param name="processId">The ID of the process that failed.</param>
    public ProcessExecutionFailedMessage(string processId)
        : base(1001, "ProcessExecutionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Process execution failed for process: {0}", processId),
               "EXEC_PROCESS_FAILED",
               "Process",
               "https://docs.Fdw.io/execution/process-management")
    { }

    /// <summary>
    /// Initializes a new instance with process ID and error details.
    /// </summary>
    /// <param name="processId">The ID of the process that failed.</param>
    /// <param name="errorDetails">The specific execution error details.</param>
    public ProcessExecutionFailedMessage(string processId, string errorDetails)
        : base(1001, "ProcessExecutionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Process execution failed for process: {0} with error: {1}",
                           processId, errorDetails),
               "EXEC_PROCESS_FAILED",
               "Process",
               "https://docs.Fdw.io/execution/process-management")
    { }

    /// <summary>
    /// Initializes a new instance with process ID, operation name, and error details.
    /// </summary>
    /// <param name="processId">The ID of the process that failed.</param>
    /// <param name="operationName">The name of the operation that failed.</param>
    /// <param name="errorDetails">The specific execution error details.</param>
    public ProcessExecutionFailedMessage(string processId, string operationName, string errorDetails)
        : base(1001, "ProcessExecutionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Process execution failed for process: {0}, operation: {1} with error: {2}",
                           processId, operationName, errorDetails),
               "EXEC_PROCESS_FAILED",
               "Process",
               "https://docs.Fdw.io/execution/process-management")
    { }
}