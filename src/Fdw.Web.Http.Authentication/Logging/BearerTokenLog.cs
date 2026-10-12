namespace Fdw.Web.Http.Authentication.Logging;

using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// MessageLogging for bearer token HTTP handler operations.
/// EventId range: 4420-4425
/// </summary>
[MessageLoggingTypeCode("AUTHENTICATION2")]
public static partial class BearerTokenLog
{
    /// <summary>
    /// Logged at Trace level when a bearer token is attached to an outgoing request.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Bearer token attached to request for {path}")]
    public static partial IGenericMessage TokenAttached(
        ILogger logger,
        string path);

    /// <summary>
    /// Logged at Warning level when no access token is available for the request — the request is
    /// dispatched with no Authorization header, so the downstream call is going out unauthenticated.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Warning,
        Message = "No access token available for request to {path}")]
    public static partial IGenericMessage NoTokenAvailable(
        ILogger logger,
        string path);

    /// <summary>
    /// Logged at Information level when a 401 response triggers a token refresh attempt.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Received 401 Unauthorized for {path}, attempting token refresh")]
    public static partial IGenericMessage UnauthorizedRefreshAttempt(
        ILogger logger,
        string path);

    /// <summary>
    /// Logged at Information level when a token refresh succeeds and the request is retried.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Token refreshed successfully, retrying request to {path}")]
    public static partial IGenericMessage TokenRefreshedRetrying(
        ILogger logger,
        string path);

    /// <summary>
    /// Logged at Warning level when a token refresh fails and the 401 response is returned.
    /// </summary>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Warning,
        Message = "Token refresh failed for request to {path}, returning 401")]
    public static partial IGenericMessage TokenRefreshFailed(
        ILogger logger,
        string path);

    /// <summary>
    /// Logged at Error level when an unexpected error occurs while attaching a bearer token.
    /// </summary>
    [MessageLogging(
        EventId = 51001,
        Level = LogLevel.Error,
        Message = "Error attaching bearer token for request to {path}")]
    public static partial IGenericMessage TokenAttachmentError(
        ILogger logger,
        Exception ex,
        string path);

    /// <summary>
    /// Logged at Warning level when a session expiry notification is sent after a failed token refresh.
    /// </summary>
    [MessageLogging(
        EventId = 51002,
        Level = LogLevel.Warning,
        Message = "Session expired notification sent for request to {path}")]
    public static partial IGenericMessage SessionExpiredNotificationSent(
        ILogger logger,
        string path);

    /// <summary>
    /// Logged at Trace level when a refresh is skipped because another caller recently completed one.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Trace,
        Message = "Refresh skipped -- another caller refreshed {elapsedMs}ms ago")]
    public static partial IGenericMessage RefreshSkippedRecentlyCompleted(
        ILogger logger,
        long elapsedMs);

    /// <summary>
    /// Logged at Debug level when the refresh coordinator acquires the gate and executes a refresh.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Refresh coordinator acquired gate, executing refresh")]
    public static partial IGenericMessage RefreshCoordinatorExecuting(
        ILogger logger);
}
