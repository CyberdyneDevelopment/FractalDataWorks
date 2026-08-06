using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Process;

/// <summary>
/// CurrentMessage indicating that a process state transition failed.
/// </summary>
[Message("ProcessStateTransitionFailed")]
public sealed class ProcessStateTransitionFailedMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessStateTransitionFailedMessage"/> class.
    /// </summary>
    public ProcessStateTransitionFailedMessage()
        : base(1004, "ProcessStateTransitionFailed", MessageSeverity.Error,
               "Process state transition failed",
               "EXEC_STATE_TRANSITION_FAILED",
               "Process",
               "https://docs.Fdw.io/execution/state-management")
    { }

    /// <summary>
    /// Initializes a new instance with process ID.
    /// </summary>
    /// <param name="processId">The ID of the process with failed state transition.</param>
    public ProcessStateTransitionFailedMessage(string processId)
        : base(1004, "ProcessStateTransitionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Process state transition failed for process: {0}", processId),
               "EXEC_STATE_TRANSITION_FAILED",
               "Process",
               "https://docs.Fdw.io/execution/state-management")
    { }

    /// <summary>
    /// Initializes a new instance with process ID and state transition details.
    /// </summary>
    /// <param name="processId">The ID of the process with failed state transition.</param>
    /// <param name="fromState">The current state of the process.</param>
    /// <param name="toState">The target state that couldn't be reached.</param>
    public ProcessStateTransitionFailedMessage(string processId, string fromState, string toState)
        : base(1004, "ProcessStateTransitionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Process state transition failed for process: {0} from '{1}' to '{2}'",
                           processId, fromState, toState),
               "EXEC_STATE_TRANSITION_FAILED",
               "Process",
               "https://docs.Fdw.io/execution/state-management")
    { }

    /// <summary>
    /// Initializes a new instance with complete transition details and error reason.
    /// </summary>
    /// <param name="processId">The ID of the process with failed state transition.</param>
    /// <param name="fromState">The current state of the process.</param>
    /// <param name="toState">The target state that couldn't be reached.</param>
    /// <param name="reason">The reason why the state transition failed.</param>
    public ProcessStateTransitionFailedMessage(string processId, string fromState, string toState, string reason)
        : base(1004, "ProcessStateTransitionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Process state transition failed for process: {0} from '{1}' to '{2}': {3}",
                           processId, fromState, toState, reason),
               "EXEC_STATE_TRANSITION_FAILED",
               "Process",
               "https://docs.Fdw.io/execution/state-management")
    { }
}