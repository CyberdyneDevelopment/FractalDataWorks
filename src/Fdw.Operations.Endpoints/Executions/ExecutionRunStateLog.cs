using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// MessageLogging for cancelling, pausing and resuming an execution (FDW-2950).
/// EventId ranges: 21050 (the caller cannot be named, 400), 31050 (no such execution, 404), 41050 (the execution has finished, 409).
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS3")]
public static partial class ExecutionRunStateLog
{
    /// <summary>Logs a change of an execution's state by a caller whose token carries no user id.</summary>
    [MessageLogging(EventId = 21050, Level = LogLevel.Warning,
        Message = "Execution {executionId} cannot be told to {verb}: the caller's token carries no user id",
        ClientMessage = "An execution is told to {verb} by a person; the token carries no user id")]
    public static partial IGenericMessage ActorUnknown(ILogger logger, Guid executionId, string verb);

    /// <summary>Logs a change of the state of an execution that does not exist.</summary>
    [MessageLogging(EventId = 31050, Level = LogLevel.Warning,
        Message = "Execution {executionId} cannot be told to {verb}: there is no such execution",
        ClientMessage = "There is no execution {executionId}")]
    public static partial IGenericMessage ExecutionNotFound(ILogger logger, Guid executionId, string verb);

    /// <summary>Logs a change of the state of an execution that has finished.</summary>
    [MessageLogging(EventId = 41050, Level = LogLevel.Warning,
        Message = "Execution {executionId} cannot be told to {verb}: it is already {state}",
        ClientMessage = "The execution cannot be told to {verb}: it is already {state}")]
    public static partial IGenericMessage AlreadyFinished(ILogger logger, Guid executionId, string verb, string state);
}
