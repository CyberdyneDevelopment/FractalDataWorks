using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging shared by every <c>IAuthenticationExecutionStore</c> implementation
/// (<c>InMemoryExecutionStore</c>, <c>DatabaseExecutionStore</c>).
/// </summary>
/// <remarks>
/// EventId range: 91130–91150. A resume token never appears here at any level. The token is the
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
        ILogger logger, Guid executionId, string flowName, int stepIndex);

    /// <summary>A resume token was consumed and its flow will continue.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91131, Level = LogLevel.Trace,
        Message = "Execution {executionId} consumed for flow '{flowName}'")]
    internal static partial IGenericMessage Consumed(
        ILogger logger, Guid executionId, string flowName);

    /// <summary>Nothing was suspended under the token presented.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91132, Level = LogLevel.Warning,
        Message = "No suspended flow can be resumed with the token presented")]
    internal static partial IGenericMessage NotResumable(ILogger logger);

    /// <summary>A record was found but had expired.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    [MessageLogging(EventId = 91133, Level = LogLevel.Debug,
        Message = "Execution {executionId} had expired and was discarded on consumption")]
    internal static partial IGenericMessage Expired(
        ILogger logger, Guid executionId);

    /// <summary>No record was supplied to suspend.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91134, Level = LogLevel.Error,
        Message = "A record must be supplied to suspend a flow")]
    internal static partial IGenericMessage RecordMissing(ILogger logger);

    /// <summary>No token was supplied to consume.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91135, Level = LogLevel.Error,
        Message = "A resume token must be supplied to consume an execution")]
    internal static partial IGenericMessage TokenMissing(ILogger logger);

    /// <summary>A record was found but had already been consumed once.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <remarks>
    /// Logged distinctly for operator visibility, but the caller still receives the same generic
    /// <see cref="NotResumable"/> failure — the same reasoning <c>InMemoryExecutionStore</c> applies to
    /// an expired record applies here: a real, already-spent token must fail exactly as a token that
    /// never existed does.
    /// </remarks>
    [MessageLogging(EventId = 91146, Level = LogLevel.Debug,
        Message = "Execution {executionId} was already consumed")]
    internal static partial IGenericMessage AlreadyConsumed(ILogger logger, Guid executionId);

    /// <summary>The atomic consume update matched zero rows — a concurrent consumer won the race.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    [MessageLogging(EventId = 91147, Level = LogLevel.Debug,
        Message = "Execution {executionId} lost the race to consume: another caller consumed it first")]
    internal static partial IGenericMessage ConsumeRaceLost(ILogger logger, Guid executionId);

    /// <summary>
    /// The best-effort replay tombstone write failed after a successful consume.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <param name="errorMessage">Why the write failed.</param>
    /// <remarks>
    /// Logged, not failed: the single-use guarantee already came from the atomic consume update, not
    /// from this write. This table exists purely so a replay is still distinguishable from a token
    /// that never existed after the execution row itself is eventually swept — losing this write once
    /// must not fail a login that had already, correctly, succeeded.
    /// </remarks>
    [MessageLogging(EventId = 91148, Level = LogLevel.Error,
        Message = "Execution {executionId} consumed, but recording its replay tombstone failed: {errorMessage}")]
    internal static partial IGenericMessage ReplayTombstoneWriteFailed(ILogger logger, Guid executionId, string errorMessage);

    /// <summary>A database operation against the execution store failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operation">Which operation failed (e.g. "Suspend", "TryConsume").</param>
    /// <param name="errorMessage">Why it failed.</param>
    [MessageLogging(EventId = 91149, Level = LogLevel.Error,
        Message = "Execution store {operation} failed: {errorMessage}")]
    internal static partial IGenericMessage PersistenceFailed(ILogger logger, string operation, string errorMessage);

    /// <summary>Encrypting or decrypting an execution's context failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operation">"Protect" or "Unprotect".</param>
    /// <param name="reason">The inner failure message.</param>
    [MessageLogging(EventId = 91150, Level = LogLevel.Error,
        Message = "Execution context {operation} failed: {reason}")]
    internal static partial IGenericMessage ProtectionFailed(ILogger logger, string operation, string reason);
}
