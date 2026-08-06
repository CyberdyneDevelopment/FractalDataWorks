using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SessionState.Clients.Logging;

/// <summary>
/// MessageLogging for the HTTP-backed session state client.
/// EventId range: 8895-8899
/// </summary>
[MessageLoggingTypeCode("CLIENTS")]
public static partial class SessionStateClientLog
{
    /// <summary>Logs a session state GET failure for the given key.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Session state GET failed for key '{key}' with status {statusCode}")]
    public static partial IGenericMessage GetFailed(ILogger logger, string key, int statusCode);

    /// <summary>Logs a session state PUT failure for the given key.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Session state PUT failed for key '{key}' with status {statusCode}")]
    public static partial IGenericMessage SaveFailed(ILogger logger, string key, int statusCode);

    /// <summary>Logs a session state DELETE failure for the given key.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Session state DELETE failed for key '{key}' with status {statusCode}")]
    public static partial IGenericMessage DeleteFailed(ILogger logger, string key, int statusCode);

    /// <summary>Logs a session state keys GET failure.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Session state keys GET failed with status {statusCode}")]
    public static partial IGenericMessage GetAllKeysFailed(ILogger logger, int statusCode);

    /// <summary>Logs a session state clear failure.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error, Message = "Session state clear failed with status {statusCode}")]
    public static partial IGenericMessage ClearAllFailed(ILogger logger, int statusCode);
}
