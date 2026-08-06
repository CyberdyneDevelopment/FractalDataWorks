using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SessionState.Logging;

/// <summary>
/// MessageLogging for session state operations.
/// EventId range: 8850-8899.
/// </summary>
[MessageLoggingTypeCode("SESSIONSTATE")]
public static partial class SessionStateLog
{
    // ── Success Operations (8850-8859) ──────────────────────────────────────

    /// <summary>Logs successful state save.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug, Message = "Session state saved for user {userId}, key {key}")]
    public static partial IGenericMessage StateSaved(ILogger logger, string userId, string key);

    /// <summary>Logs successful state retrieval.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Debug, Message = "Session state retrieved for user {userId}, key {key}")]
    public static partial IGenericMessage StateRetrieved(ILogger logger, string userId, string key);

    /// <summary>Logs successful state deletion.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Debug, Message = "Session state deleted for user {userId}, key {key}")]
    public static partial IGenericMessage StateDeleted(ILogger logger, string userId, string key);

    /// <summary>Logs all keys retrieval.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Debug, Message = "Retrieved {count} session state keys for user {userId}")]
    public static partial IGenericMessage AllKeysRetrieved(ILogger logger, string userId, int count);

    /// <summary>Logs clearing all state for a user.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Information, Message = "All session state cleared for user {userId}")]
    public static partial IGenericMessage AllStateCleared(ILogger logger, string userId);

    /// <summary>Logs expired state cleanup.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "Cleaned {count} expired session state entries")]
    public static partial IGenericMessage ExpiredStatesCleaned(ILogger logger, int count);

    // ── State Not Found (8860) ──────────────────────────────────────────────

    /// <summary>Logs state not found.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Debug, Message = "Session state not found for user {userId}, key {key}")]
    public static partial IGenericMessage StateNotFound(ILogger logger, string userId, string key);

    // ── Errors (8870-8879) ──────────────────────────────────────────────────

    /// <summary>Logs state save failure.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error, Message = "Failed to save session state for user {userId}, key {key}: {error}")]
    public static partial IGenericMessage SaveStateFailed(ILogger logger, string userId, string key, string error);

    /// <summary>Logs state retrieval failure.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error, Message = "Failed to get session state for user {userId}, key {key}: {error}")]
    public static partial IGenericMessage GetStateFailed(ILogger logger, string userId, string key, string error);

    /// <summary>Logs state deletion failure.</summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error, Message = "Failed to delete session state for user {userId}, key {key}: {error}")]
    public static partial IGenericMessage DeleteStateFailed(ILogger logger, string userId, string key, string error);

    /// <summary>Logs all keys retrieval failure.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error, Message = "Failed to get session state keys for user {userId}: {error}")]
    public static partial IGenericMessage GetAllKeysFailed(ILogger logger, string userId, string error);

    /// <summary>Logs clear all failure.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error, Message = "Failed to clear session state for user {userId}: {error}")]
    public static partial IGenericMessage ClearAllFailed(ILogger logger, string userId, string error);

    /// <summary>Logs deserialization failure.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to deserialize session state for user {userId}, key {key}: {error}")]
    public static partial IGenericMessage DeserializationFailed(ILogger logger, string userId, string key, string error);

    // ── Trace Entry Points (8880-8889) ──────────────────────────────────────

    /// <summary>Trace entry for SaveStateAsync.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace, Message = "Entering SaveStateAsync")]
    public static partial IGenericMessage TraceSaveStateEntry(ILogger logger);

    /// <summary>Trace entry for GetStateAsync.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace, Message = "Entering GetStateAsync")]
    public static partial IGenericMessage TraceGetStateEntry(ILogger logger);

    /// <summary>Trace entry for DeleteStateAsync.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace, Message = "Entering DeleteStateAsync")]
    public static partial IGenericMessage TraceDeleteStateEntry(ILogger logger);

    /// <summary>Trace entry for GetAllKeysAsync.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace, Message = "Entering GetAllKeysAsync")]
    public static partial IGenericMessage TraceGetAllKeysEntry(ILogger logger);

    /// <summary>Trace entry for ClearAllAsync.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Entering ClearAllAsync")]
    public static partial IGenericMessage TraceClearAllEntry(ILogger logger);

    /// <summary>Trace entry for CleanExpiredAsync.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace, Message = "Entering CleanExpiredAsync")]
    public static partial IGenericMessage TraceCleanExpiredEntry(ILogger logger);

    /// <summary>Trace entry for circuit opened.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Entering OnConnectionUpAsync")]
    public static partial IGenericMessage TraceCircuitOpenedEntry(ILogger logger);

    /// <summary>Trace entry for circuit closed.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Trace, Message = "Entering OnCircuitClosedAsync")]
    public static partial IGenericMessage TraceCircuitClosedEntry(ILogger logger);

    // ── Circuit Handler Operations (8890-8899) ──────────────────────────────

    /// <summary>Logs state loaded on circuit open.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Debug, Message = "Circuit state loaded for user {userId}: {count} keys")]
    public static partial IGenericMessage CircuitStateLoaded(ILogger logger, string userId, int count);

    /// <summary>Logs dirty state persisted on circuit close.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Debug, Message = "Circuit state persisted for user {userId}: {count} dirty keys")]
    public static partial IGenericMessage CircuitStatePersisted(ILogger logger, string userId, int count);
}
