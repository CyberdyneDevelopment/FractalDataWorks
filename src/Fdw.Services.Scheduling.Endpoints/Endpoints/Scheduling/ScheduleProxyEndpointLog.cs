using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// MessageLogging definitions for scheduling proxy endpoint operations.
/// EventId range: 7246-7255
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS10")]
public static partial class ScheduleProxyEndpointLog
{
    /// <summary>Logs a proxy request being sent.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Proxying {method} to {service}: {path}")]
    public static partial IGenericMessage ProxyRequest(ILogger logger, string method, string service, string path);

    /// <summary>Logs a successful proxy response.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Proxy response from {service}: {statusCode}")]
    public static partial IGenericMessage ProxyResponse(ILogger logger, string service, int statusCode);

    /// <summary>Logs a proxy failure.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Proxy call to {service} failed: {error}")]
    public static partial IGenericMessage ProxyFailed(ILogger logger, string service, string error);
}
