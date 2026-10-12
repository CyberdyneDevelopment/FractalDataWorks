using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Execution;
using Microsoft.Extensions.Logging;
using System;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for <see cref="DatabaseExecutionStore"/>.
/// </summary>
/// <remarks>
/// EventId range: 91151–91159. A resume token never appears here at any level, for the same reason as
/// <see cref="ExecutionStoreLog"/>.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class DatabaseExecutionStoreLog
{
    /// <summary>A flow was suspended and can be resumed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepIndex">The step it suspended at.</param>
    [MessageLogging(EventId = 91151, Level = LogLevel.Trace,
        Message = "Execution {executionId} suspended for flow '{flowName}' at step {stepIndex}")]
    internal static partial IGenericMessage Suspended(
        ILogger<DatabaseExecutionStore> logger, Guid executionId, string flowName, int stepIndex);

    /// <summary>A resume token was consumed and its flow will continue.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91152, Level = LogLevel.Trace,
        Message = "Execution {executionId} consumed for flow '{flowName}'")]
    internal static partial IGenericMessage Consumed(
        ILogger<DatabaseExecutionStore> logger, Guid executionId, string flowName);

    /// <summary>Nothing was suspended under the token presented, it had already been consumed, or it had expired.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91153, Level = LogLevel.Warning,
        Message = "No suspended flow can be resumed with the token presented")]
    internal static partial IGenericMessage NotResumable(ILogger<DatabaseExecutionStore> logger);

    /// <summary>No record was supplied to suspend.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91154, Level = LogLevel.Error,
        Message = "A record must be supplied to suspend a flow")]
    internal static partial IGenericMessage RecordMissing(ILogger<DatabaseExecutionStore> logger);

    /// <summary>No token was supplied to consume.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91155, Level = LogLevel.Error,
        Message = "A resume token must be supplied to consume an execution")]
    internal static partial IGenericMessage TokenMissing(ILogger<DatabaseExecutionStore> logger);

    /// <summary>Writing the suspended execution row failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    [MessageLogging(EventId = 91156, Level = LogLevel.Error,
        Message = "Execution {executionId} could not be persisted to AuthDb")]
    internal static partial IGenericMessage SuspendWriteFailed(
        ILogger<DatabaseExecutionStore> logger, Guid executionId);

    /// <summary>A just-consumed execution row could not be read back.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91157, Level = LogLevel.Critical,
        Message = "An execution was consumed but could not be read back from AuthDb")]
    internal static partial IGenericMessage ConsumeReadBackFailed(ILogger<DatabaseExecutionStore> logger);

    /// <summary>Recording a spent token in ConsumedResumeToken failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    [MessageLogging(EventId = 91158, Level = LogLevel.Error,
        Message = "Execution {executionId} was consumed but its ConsumedResumeToken row could not be written")]
    internal static partial IGenericMessage ConsumedTokenWriteFailed(
        ILogger<DatabaseExecutionStore> logger, Guid executionId);

    /// <summary>A consumed execution's decrypted ContextData did not deserialize.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    [MessageLogging(EventId = 91159, Level = LogLevel.Critical,
        Message = "Execution {executionId} was consumed but its ContextData did not deserialize")]
    internal static partial IGenericMessage ContextDataUnreadable(
        ILogger<DatabaseExecutionStore> logger, Guid executionId);

    /// <summary>
    /// The flow names no execution key, so what it stores cannot be encrypted or read. The provider
    /// only loads a keyless flow that stores nothing; reaching this means a step suspended, or a code
    /// was asked for, in a flow configured as never doing either (FDW-897).
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 61009, Level = LogLevel.Error,
        Message = "Flow '{flowName}' names no execution key (SecretManagerName/SecretKeyName), so its suspended execution cannot be encrypted or read")]
    internal static partial IGenericMessage NoExecutionKey(
        ILogger<DatabaseExecutionStore> logger, string flowName);
}
