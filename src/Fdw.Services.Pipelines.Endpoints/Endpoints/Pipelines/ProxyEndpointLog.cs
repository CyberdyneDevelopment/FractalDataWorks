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

    /// <summary>Logs an ETL webhook receipt.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "ETL webhook received: execution {executionId} status {status}")]
    public static partial IGenericMessage EtlWebhookReceived(ILogger logger, string executionId, string status);

    /// <summary>Logs an ETL webhook with unknown execution ID.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "ETL webhook received for unknown execution {executionId}")]
    public static partial IGenericMessage EtlWebhookUnknownExecution(ILogger logger, string executionId);
}
