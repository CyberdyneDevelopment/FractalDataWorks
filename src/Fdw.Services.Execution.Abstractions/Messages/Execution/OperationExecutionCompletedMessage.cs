using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Execution;

/// <summary>
/// CurrentMessage indicating that an operation execution has completed successfully.
/// </summary>
[Message("OperationExecutionCompleted")]
public sealed class OperationExecutionCompletedMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationExecutionCompletedMessage"/> class.
    /// </summary>
    public OperationExecutionCompletedMessage()
        : base(3003, "OperationExecutionCompleted", MessageSeverity.Information,
               "Operation execution completed successfully",
               "EXEC_OPERATION_COMPLETED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name.
    /// </summary>
    /// <param name="operationName">The name of the completed operation.</param>
    public OperationExecutionCompletedMessage(string operationName)
        : base(3003, "OperationExecutionCompleted", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' completed successfully", operationName),
               "EXEC_OPERATION_COMPLETED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name and process ID.
    /// </summary>
    /// <param name="operationName">The name of the completed operation.</param>
    /// <param name="processId">The ID of the process that executed the operation.</param>
    public OperationExecutionCompletedMessage(string operationName, string processId)
        : base(3003, "OperationExecutionCompleted", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' completed successfully for process: {1}",
                           operationName, processId),
               "EXEC_OPERATION_COMPLETED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name, process ID, and duration.
    /// </summary>
    /// <param name="operationName">The name of the completed operation.</param>
    /// <param name="processId">The ID of the process that executed the operation.</param>
    /// <param name="durationMs">The duration of the operation in milliseconds.</param>
    public OperationExecutionCompletedMessage(string operationName, string processId, long durationMs)
        : base(3003, "OperationExecutionCompleted", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' completed successfully for process: {1} in {2}ms",
                           operationName, processId, durationMs),
               "EXEC_OPERATION_COMPLETED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }
}