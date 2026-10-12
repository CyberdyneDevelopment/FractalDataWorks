namespace Fdw.Web.Http.Authentication.Logging;

using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// MessageLogging for API key HTTP handler operations.
/// EventId range: 4430-4435
/// </summary>
[MessageLoggingTypeCode("AUTHENTICATION2")]
public static partial class ApiKeyLog
{
    /// <summary>
    /// Logged at Trace level when an API key is attached to an outgoing request.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "API key attached to request for {path}")]
    public static partial IGenericMessage ApiKeyAttached(
        ILogger logger,
        string path);

    /// <summary>
    /// Logged at Warning level when no API key is available for the request — the request is
    /// dispatched with no API key, so the downstream call is going out unauthenticated.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Warning,
        Message = "No API key available for request to {path}")]
    public static partial IGenericMessage NoApiKeyAvailable(
        ILogger logger,
        string path);
}
