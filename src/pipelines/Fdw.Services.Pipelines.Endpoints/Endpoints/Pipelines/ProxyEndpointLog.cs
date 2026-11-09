using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// MessageLogging definitions for pipeline proxy endpoint operations.
/// EventId range: 7232-7245
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS9")]
public static partial class ProxyEndpointLog
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

    /// <summary>Logs that the request failed: Failed to proxy request to EtlServer.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Failed to proxy request to EtlServer", ClientMessage = "Failed to proxy request to EtlServer")]
    public static partial IGenericMessage TriggerEtlJobProxyFailedToProxyRequestTo(ILogger logger);
}
