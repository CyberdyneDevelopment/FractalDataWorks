using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages.Execution;

/// <summary>
/// CurrentMessage indicating that a requested operation is not supported by the process.
/// </summary>
[Message("OperationNotSupported")]
public sealed class OperationNotSupportedMessage : ExecutionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationNotSupportedMessage"/> class.
    /// </summary>
    public OperationNotSupportedMessage()
        : base(3001, "OperationNotSupported", MessageSeverity.Error,
               "Operation is not supported",
               "EXEC_OPERATION_NOT_SUPPORTED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name.
    /// </summary>
    /// <param name="operationName">The name of the unsupported operation.</param>
    public OperationNotSupportedMessage(string operationName)
        : base(3001, "OperationNotSupported", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' is not supported", operationName),
               "EXEC_OPERATION_NOT_SUPPORTED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name and process type.
    /// </summary>
    /// <param name="operationName">The name of the unsupported operation.</param>
    /// <param name="processType">The type of process that doesn't support the operation.</param>
    public OperationNotSupportedMessage(string operationName, string processType)
        : base(3001, "OperationNotSupported", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' is not supported by process type: {1}",
                           operationName, processType),
               "EXEC_OPERATION_NOT_SUPPORTED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }

    /// <summary>
    /// Initializes a new instance with operation name, process ID, and process type.
    /// </summary>
    /// <param name="operationName">The name of the unsupported operation.</param>
    /// <param name="processId">The ID of the process that doesn't support the operation.</param>
    /// <param name="processType">The type of process that doesn't support the operation.</param>
    public OperationNotSupportedMessage(string operationName, string processId, string processType)
        : base(3001, "OperationNotSupported", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Operation '{0}' is not supported by process: {1} (type: {2})",
                           operationName, processId, processType),
               "EXEC_OPERATION_NOT_SUPPORTED",
               "Execution",
               "https://docs.Fdw.io/execution/operations")
    { }
}