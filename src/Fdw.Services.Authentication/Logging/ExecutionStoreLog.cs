using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Execution;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for the in-memory execution store.
/// </summary>
/// <remarks>
/// EventId range: 91130–91135. A resume token never appears here at any level. The token is the
/// credential for a half-finished login, and a log is the wrong place for one — an execution is
/// identified by its own id instead, which correlates just as well and grants nothing.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class ExecutionStoreLog
{
    /// <summary>A flow was suspended and can be resumed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepIndex">The step it suspended at.</param>
    [MessageLogging(EventId = 91130, Level = LogLevel.Trace,
        Message = "Execution {executionId} suspended for flow '{flowName}' at step {stepIndex}")]
    internal static partial IGenericMessage Suspended(
        ILogger<InMemoryExecutionStore> logger, Guid executionId, string flowName, int stepIndex);

    /// <summary>A resume token was consumed and its flow will continue.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91131, Level = LogLevel.Trace,
        Message = "Execution {executionId} consumed for flow '{flowName}'")]
    internal static partial IGenericMessage Consumed(
        ILogger<InMemoryExecutionStore> logger, Guid executionId, string flowName);

    /// <summary>Nothing was suspended under the token presented.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91132, Level = LogLevel.Warning,
        Message = "No suspended flow can be resumed with the token presented")]
    internal static partial IGenericMessage NotResumable(ILogger<InMemoryExecutionStore> logger);

    /// <summary>A record was found but had expired.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    [MessageLogging(EventId = 91133, Level = LogLevel.Debug,
        Message = "Execution {executionId} had expired and was discarded on consumption")]
    internal static partial IGenericMessage Expired(
        ILogger<InMemoryExecutionStore> logger, Guid executionId);

    /// <summary>No record was supplied to suspend.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91134, Level = LogLevel.Error,
        Message = "A record must be supplied to suspend a flow")]
    internal static partial IGenericMessage RecordMissing(ILogger<InMemoryExecutionStore> logger);

    /// <summary>No token was supplied to consume.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91135, Level = LogLevel.Error,
        Message = "A resume token must be supplied to consume an execution")]
    internal static partial IGenericMessage TokenMissing(ILogger<InMemoryExecutionStore> logger);
}
