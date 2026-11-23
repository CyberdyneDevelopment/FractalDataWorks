using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for the operation rule (FDW-6682).
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataOperationLog
{
    /// <summary>
    /// Logs that a command was refused because its container's type does not state an operation the command performs.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The container the command was run against.</param>
    /// <param name="containerTypeName">The container's type.</param>
    /// <param name="operation">The operation the command performs that the type does not state.</param>
    /// <param name="statedOperations">The operations the type states, joined, or none.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> the refused command carries.</returns>
    [MessageLogging(EventId = 61148, Level = LogLevel.Error,
        Message = "[DataOperationRule] Container '{containerName}' is of type '{containerTypeName}', which does not state the operation '{operation}' (it states: {statedOperations}), so the command was refused and nothing was sent")]
    public static partial IGenericMessage OperationNotStated(ILogger logger, string containerName, string containerTypeName, string operation, string statedOperations);
}
