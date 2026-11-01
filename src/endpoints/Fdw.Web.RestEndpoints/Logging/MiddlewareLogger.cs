using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Web.RestEndpoints.Logging;

/// <summary>
/// Static logger class for middleware pipeline operations.
/// EventId range: 8020-8050
/// </summary>
[MessageLoggingTypeCode("RESTENDPOINTS")]
public static partial class MiddlewareLogger
{
    /// <summary>
    /// Logs when the middleware pipeline has been configured.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information, Message = "Fdw middleware pipeline configured with {middlewareCount} middleware components")]
    public static partial IGenericMessage MiddlewarePipelineConfigured(ILogger logger, int middlewareCount);

    /// <summary>
    /// Logs the state of a middleware component.
    /// </summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Debug, Message = "Middleware '{middlewareName}' is {state}")]
    public static partial IGenericMessage MiddlewareState(ILogger logger, string middlewareName, string state);

    /// <summary>
    /// Logs when a required service is not registered.
    /// </summary>
    [MessageLogging(EventId = 61000, Level = LogLevel.Warning, Message = "Required service '{serviceName}' is not registered")]
    public static partial IGenericMessage RequiredServiceMissing(ILogger logger, string serviceName);

    /// <summary>
    /// Logs when a request has completed.
    /// </summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "Request {method} {path} completed in {duration}ms with status {statusCode}")]
    public static partial IGenericMessage RequestCompleted(ILogger logger, string method, string path, double duration, int statusCode);

    /// <summary>
    /// Logs when request validation fails.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning, Message = "Request validation failed: {reason}")]
    public static partial IGenericMessage RequestValidationFailed(ILogger logger, string reason);

    /// <summary>
    /// Logs when an unhandled exception occurs in the pipeline.
    /// </summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Unhandled exception in request pipeline")]
    public static partial IGenericMessage UnhandledException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs when a health check endpoint is registered.
    /// </summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "Health check endpoint registered at {path}")]
    public static partial IGenericMessage HealthCheckRegistered(ILogger logger, string path);

    /// <summary>
    /// Logs a configuration warning.
    /// </summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Warning, Message = "Web configuration validation warning: {warning}")]
    public static partial IGenericMessage ConfigurationWarning(ILogger logger, string warning);
}
