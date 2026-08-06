namespace Fdw.Web.Clients.Abstractions;

using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// MessageLogging for HTTP API client operations.
/// </summary>
/// <remarks>
/// Every message carries the ABSOLUTE request URI (base address + path) — a relative path alone
/// hides which host/port the client actually targeted, which is exactly the detail needed when a
/// misconfigured base address is the fault (a stale <c>localhost:5002</c> was invisible for three
/// diagnosis cycles before this). Exception-carrying messages also inline <c>{error}</c>
/// (<see cref="Exception.Message"/>) so the single failure line names its cause — the flattened
/// message flows into the returned <see cref="IGenericMessage"/> and is what upstream services
/// (e.g. the scheduler's dispatch retry) surface.
/// </remarks>
[MessageLoggingTypeCode("ABSTRACTIONS9")]
public static partial class ClientLog
{
    /// <summary>
    /// Logged at Trace level before sending an HTTP request.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Sending {method} request to {uri}")]
    public static partial IGenericMessage SendingRequest(
        ILogger logger,
        string method,
        string uri);

    /// <summary>
    /// Logged at Debug level when an HTTP response is received.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "{method} request to {uri} received response {statusCode}")]
    public static partial IGenericMessage ResponseReceived(
        ILogger logger,
        string method,
        string uri,
        int statusCode);

    /// <summary>
    /// Logged at Information level when an HTTP request completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "{method} request to {uri} completed successfully")]
    public static partial IGenericMessage RequestCompleted(
        ILogger logger,
        string method,
        string uri);

    /// <summary>
    /// Logged at Warning level when an HTTP request returns a non-success status code.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "{method} request to {uri} returned non-success status {statusCode}")]
    public static partial IGenericMessage RequestNonSuccess(
        ILogger logger,
        string method,
        string uri,
        int statusCode);

    /// <summary>
    /// Logged at Warning level when an HTTP request returns a non-success status code,
    /// including the server's response body so the real failure reason is preserved.
    /// </summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "{method} request to {uri} returned non-success status {statusCode}: {detail}")]
    public static partial IGenericMessage RequestNonSuccessDetail(
        ILogger logger,
        string method,
        string uri,
        int statusCode,
        string detail);

    /// <summary>
    /// Logged at Error level when an HTTP request fails due to a network or protocol error —
    /// the flattened <paramref name="error"/> names the transport cause (connection refused,
    /// name resolution, TLS, timeout) in the message itself.
    /// </summary>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "{method} request to {uri} failed with HTTP error: {error}")]
    public static partial IGenericMessage HttpRequestFailed(
        ILogger logger,
        Exception ex,
        string method,
        string uri,
        string error);

    /// <summary>
    /// Logged at Error level when JSON serialization or deserialization fails.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "{method} request to {uri} failed with serialization error: {error}")]
    public static partial IGenericMessage DeserializationFailed(
        ILogger logger,
        Exception ex,
        string method,
        string uri,
        string error);

    /// <summary>
    /// Logged at Error level when an unexpected exception occurs during an HTTP request —
    /// the unknown is caught, flattened into <paramref name="error"/>, and RETURNED in the
    /// failure result (never rethrown).
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "{method} request to {uri} failed with unexpected error: {error}")]
    public static partial IGenericMessage UnexpectedError(
        ILogger logger,
        Exception ex,
        string method,
        string uri,
        string error);

    /// <summary>
    /// Logged at Error level when a 2xx response deserializes to a null body,
    /// so the failure names the real condition instead of a fabricated status code.
    /// </summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "{method} request to {uri} returned a null response body")]
    public static partial IGenericMessage NullResponseBody(
        ILogger logger,
        string method,
        string uri);

    /// <summary>
    /// Logged at Error level when a list response is neither a JSON array nor an
    /// items envelope — a shape mismatch, not an exception, so no Exception parameter.
    /// </summary>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "{method} response from {uri} was neither an array nor an items envelope")]
    public static partial IGenericMessage ResponseShapeUnrecognized(
        ILogger logger,
        string method,
        string uri);
}
