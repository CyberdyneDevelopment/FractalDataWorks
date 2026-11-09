using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Endpoints.Logging;

/// <summary>
/// MessageLogging for session state endpoint operations.
/// EventId range: 7261-7280
/// </summary>
[MessageLoggingTypeCode("WEBENDPOINTS")]
public static partial class SessionStateEndpointLog
{
    /// <summary>Logs listing session state keys.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Listing session state keys for user '{userId}'")]
    public static partial IGenericMessage ListingKeys(ILogger logger, string userId);

    /// <summary>Logs getting a session state value.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Getting session state key '{key}' for user '{userId}'")]
    public static partial IGenericMessage GettingState(ILogger logger, string userId, string key);

    /// <summary>Logs upserting a session state value.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Upserting session state key '{key}' for user '{userId}'")]
    public static partial IGenericMessage UpsertingState(ILogger logger, string userId, string key);

    /// <summary>Logs deleting a session state value.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "Deleting session state key '{key}' for user '{userId}'")]
    public static partial IGenericMessage DeletingState(ILogger logger, string userId, string key);

    /// <summary>Logs clearing all session state for a user.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "Clearing all session state for user '{userId}'")]
    public static partial IGenericMessage ClearingAll(ILogger logger, string userId);

    /// <summary>Logs a session state operation failure.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Session state operation failed: {operation}")]
    public static partial IGenericMessage OperationFailed(ILogger logger, string operation);

    /// <summary>Logs an unexpected error in session state endpoint.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "Unexpected error in session state endpoint")]
    public static partial IGenericMessage UnexpectedError(ILogger logger, Exception exception);

    /// <summary>Logs that The request to get session state was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50001, Level = LogLevel.Warning, Message = "The request to get session state was refused: the caller is not signed in", ClientMessage = "You must be signed in to get session state")]
    public static partial IGenericMessage GetSessionStateNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to clear session state was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50002, Level = LogLevel.Warning, Message = "The request to clear session state was refused: the caller is not signed in", ClientMessage = "You must be signed in to clear session state")]
    public static partial IGenericMessage ClearSessionStateNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to list session state keys was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50003, Level = LogLevel.Warning, Message = "The request to list session state keys was refused: the caller is not signed in", ClientMessage = "You must be signed in to list session state keys")]
    public static partial IGenericMessage ListSessionStateKeysNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to upsert session state was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50004, Level = LogLevel.Warning, Message = "The request to upsert session state was refused: the caller is not signed in", ClientMessage = "You must be signed in to upsert session state")]
    public static partial IGenericMessage UpsertSessionStateNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to delete session state was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50005, Level = LogLevel.Warning, Message = "The request to delete session state was refused: the caller is not signed in", ClientMessage = "You must be signed in to delete session state")]
    public static partial IGenericMessage DeleteSessionStateNotSignedIn(ILogger logger);

    /// <summary>Logs that Session state was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Session state was not found, or is not visible to the caller", ClientMessage = "Session state was not found")]
    public static partial IGenericMessage GetSessionStateNotFound(ILogger logger);
}
