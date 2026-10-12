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
}
