using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SessionState.Logging;

/// <summary>
/// MessageLogging for SessionStateConfigurationProvider operations.
/// EventId range: 8828-8843
/// </summary>
[MessageLoggingTypeCode("SESSIONSTATE")]
public static partial class SessionStateConfigurationProviderLog
{
    // Read operations — 8828-8831
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Reading session state record for user {userId}, key {key}")]
    public static partial IGenericMessage GetRecordTrace(ILogger logger, System.Guid userId, string key);

    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Reading all session state keys for user {userId}")]
    public static partial IGenericMessage GetKeysTrace(ILogger logger, System.Guid userId);

    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Failed to query session state for user {userId}, key {key}")]
    public static partial IGenericMessage GetRecordFailed(ILogger logger, System.Guid userId, string key, System.Exception ex);

    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to query session state keys for user {userId}")]
    public static partial IGenericMessage GetKeysFailed(ILogger logger, System.Guid userId, System.Exception ex);

    // Write operations — 8832-8839
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Upserting session state for user {userId}, key {key}")]
    public static partial IGenericMessage UpsertTrace(ILogger logger, System.Guid userId, string key);

    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "Session state upserted for user {userId}, key {key}")]
    public static partial IGenericMessage UpsertSaved(ILogger logger, System.Guid userId, string key);

    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "Failed to insert session state for user {userId}, key {key}")]
    public static partial IGenericMessage InsertFailed(ILogger logger, System.Guid userId, string key, System.Exception ex);

    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "Failed to update session state for user {userId}, key {key}")]
    public static partial IGenericMessage UpdateFailed(ILogger logger, System.Guid userId, string key, System.Exception ex);

    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "Deleting session state for user {userId}, key {key}")]
    public static partial IGenericMessage DeleteTrace(ILogger logger, System.Guid userId, string key);

    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Session state deleted for user {userId}, key {key}")]
    public static partial IGenericMessage DeleteDone(ILogger logger, System.Guid userId, string key);

    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "Failed to delete session state for user {userId}, key {key}")]
    public static partial IGenericMessage DeleteFailed(ILogger logger, System.Guid userId, string key, System.Exception ex);

    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "Clearing all session state for user {userId}")]
    public static partial IGenericMessage ClearAllTrace(ILogger logger, System.Guid userId);

    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "All session state cleared for user {userId}")]
    public static partial IGenericMessage ClearAllDone(ILogger logger, System.Guid userId);

    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "Failed to clear session state for user {userId}")]
    public static partial IGenericMessage ClearAllFailed(ILogger logger, System.Guid userId, System.Exception ex);
}
