namespace Fdw.Web.Http.Authentication.Blazor.Logging;

using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// MessageLogging for Blazor Server authentication operations.
/// EventId range: 4440-4459
/// </summary>
[MessageLoggingTypeCode("BLAZOR")]
public static partial class BlazorAuthLog
{
    /// <summary>
    /// Logged at Debug level when an access token is captured during circuit connection.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Access token captured for circuit {circuitId}")]
    public static partial IGenericMessage TokenCaptured(
        ILogger logger,
        string circuitId);

    /// <summary>
    /// Logged at Warning level when no access token is found during circuit connection —
    /// the circuit lives its entire lifetime unauthenticated as a result.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Warning,
        Message = "No access token found during circuit connection {circuitId}")]
    public static partial IGenericMessage TokenNotFound(
        ILogger logger,
        string circuitId);

    /// <summary>
    /// Logged at Warning level when HttpContext is unavailable during circuit connection —
    /// an infrastructure anomaly at the WebSocket handshake with the same unauthenticated consequence.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Warning,
        Message = "HttpContext unavailable during circuit connection {circuitId}")]
    public static partial IGenericMessage NoHttpContext(
        ILogger logger,
        string circuitId);

    /// <summary>
    /// Logged at Debug level when a circuit is closed and the captured token is cleared.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Circuit {circuitId} closed, captured token cleared")]
    public static partial IGenericMessage CircuitClosed(
        ILogger logger,
        string circuitId);

    /// <summary>
    /// Logged at Trace level when a token is obtained from the circuit accessor.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "Access token obtained from circuit accessor")]
    public static partial IGenericMessage TokenFromCircuit(
        ILogger logger);

    /// <summary>
    /// Logged at Trace level when a token is obtained from HttpContext fallback.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Trace,
        Message = "Access token obtained from HttpContext fallback")]
    public static partial IGenericMessage TokenFromHttpContext(
        ILogger logger);

    /// <summary>
    /// Logged at Warning level when no access token is available from any source — this is the
    /// terminal fall-through: it returns null so every downstream API call goes out unauthenticated,
    /// producing blanket 401s.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Warning,
        Message = "No access token available from circuit accessor or HttpContext")]
    public static partial IGenericMessage NoTokenAvailable(
        ILogger logger);

    /// <summary>
    /// Logged at Error level when an error occurs retrieving a token from HttpContext.
    /// </summary>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Error,
        Message = "Error retrieving access token from HttpContext")]
    public static partial IGenericMessage TokenRetrievalError(
        ILogger logger,
        Exception ex);
}
