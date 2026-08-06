using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Execution;

/// <summary>
/// CurrentMessage indicating that an operation execution has failed.
/// </summary>
[Message("OperationExecutionFailed")]
public sealed class OperationExecutionFailedMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationExecutionFailedMessage"/> class.
    /// </summary>
    public OperationExecutionFailedMessage()
        : base(3004, "OperationExecutionFailed", MessageSeverity.Error,
               "Operation execution failed",
               "EXEC_OPERATION_FAILED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name.
    /// </summary>
    /// <param name="operationName">The name of the failed operation.</param>
    public OperationExecutionFailedMessage(string operationName)
        : base(3004, "OperationExecutionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' execution failed", operationName),
               "EXEC_OPERATION_FAILED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name and error details.
    /// </summary>
    /// <param name="operationName">The name of the failed operation.</param>
    /// <param name="errorDetails">Details about the operation failure.</param>
    public OperationExecutionFailedMessage(string operationName, string errorDetails)
        : base(3004, "OperationExecutionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' execution failed: {1}",
                           operationName, errorDetails),
               "EXEC_OPERATION_FAILED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name, process ID, and error details.
    /// </summary>
    /// <param name="operationName">The name of the failed operation.</param>
    /// <param name="processId">The ID of the process that failed to execute the operation.</param>
    /// <param name="errorDetails">Details about the operation failure.</param>
    public OperationExecutionFailedMessage(string operationName, string processId, string errorDetails)
        : base(3004, "OperationExecutionFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' execution failed for process: {1}: {2}",
                           operationName, processId, errorDetails),
               "EXEC_OPERATION_FAILED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }
}