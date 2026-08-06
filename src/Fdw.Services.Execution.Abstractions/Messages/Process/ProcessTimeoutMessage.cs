using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Process;

/// <summary>
/// CurrentMessage indicating that a process operation timed out.
/// </summary>
[Message("ProcessTimeout")]
public sealed class ProcessTimeoutMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessTimeoutMessage"/> class.
    /// </summary>
    public ProcessTimeoutMessage()
        : base(1002, "ProcessTimeout", MessageSeverity.Warning,
               "Process operation timed out",
               "EXEC_PROCESS_TIMEOUT",
               "Process",
               "https://docs.Fdw.io/execution/timeouts")
    { }

    /// <summary>
    /// Initializes a new instance with process ID.
    /// </summary>
    /// <param name="processId">The ID of the process that timed out.</param>
    public ProcessTimeoutMessage(string processId)
        : base(1002, "ProcessTimeout", MessageSeverity.Warning,
               string.Format(CultureInfo.InvariantCulture, "Process operation timed out for process: {0}", processId),
               "EXEC_PROCESS_TIMEOUT",
               "Process",
               "https://docs.Fdw.io/execution/timeouts")
    { }

    /// <summary>
    /// Initializes a new instance with process ID and timeout duration.
    /// </summary>
    /// <param name="processId">The ID of the process that timed out.</param>
    /// <param name="timeoutSeconds">The timeout duration in seconds.</param>
    public ProcessTimeoutMessage(string processId, int timeoutSeconds)
        : base(1002, "ProcessTimeout", MessageSeverity.Warning,
               string.Format(CultureInfo.InvariantCulture, "Process operation timed out for process: {0} after {1} seconds",
                           processId, timeoutSeconds),
               "EXEC_PROCESS_TIMEOUT",
               "Process",
               "https://docs.Fdw.io/execution/timeouts")
    { }

    /// <summary>
    /// Initializes a new instance with process ID, operation name, and timeout duration.
    /// </summary>
    /// <param name="processId">The ID of the process that timed out.</param>
    /// <param name="operationName">The name of the operation that timed out.</param>
    /// <param name="timeoutSeconds">The timeout duration in seconds.</param>
    public ProcessTimeoutMessage(string processId, string operationName, int timeoutSeconds)
        : base(1002, "ProcessTimeout", MessageSeverity.Warning,
               string.Format(CultureInfo.InvariantCulture, "Process operation '{0}' timed out for process: {1} after {2} seconds",
                           operationName, processId, timeoutSeconds),
               "EXEC_PROCESS_TIMEOUT",
               "Process",
               "https://docs.Fdw.io/execution/timeouts")
    { }
}