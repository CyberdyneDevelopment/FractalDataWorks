using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Execution;

/// <summary>
/// CurrentMessage indicating that an operation execution has started.
/// </summary>
[Message("OperationExecutionStarted")]
public sealed class OperationExecutionStartedMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationExecutionStartedMessage"/> class.
    /// </summary>
    public OperationExecutionStartedMessage()
        : base(3002, "OperationExecutionStarted", MessageSeverity.Information,
               "Operation execution started",
               "EXEC_OPERATION_STARTED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name.
    /// </summary>
    /// <param name="operationName">The name of the started operation.</param>
    public OperationExecutionStartedMessage(string operationName)
        : base(3002, "OperationExecutionStarted", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' execution started", operationName),
               "EXEC_OPERATION_STARTED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name and process ID.
    /// </summary>
    /// <param name="operationName">The name of the started operation.</param>
    /// <param name="processId">The ID of the process executing the operation.</param>
    public OperationExecutionStartedMessage(string operationName, string processId)
        : base(3002, "OperationExecutionStarted", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' execution started for process: {1}",
                           operationName, processId),
               "EXEC_OPERATION_STARTED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name, process ID, and process type.
    /// </summary>
    /// <param name="operationName">The name of the started operation.</param>
    /// <param name="processId">The ID of the process executing the operation.</param>
    /// <param name="processType">The type of process executing the operation.</param>
    public OperationExecutionStartedMessage(string operationName, string processId, string processType)
        : base(3002, "OperationExecutionStarted", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' execution started for process: {1} (type: {2})",
                           operationName, processId, processType),
               "EXEC_OPERATION_STARTED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }
}