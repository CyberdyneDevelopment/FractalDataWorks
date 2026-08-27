using System;
using Microsoft.Extensions.Logging;
using Fdw.MessageLogging;
using Fdw.Messages;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// Source-generated logging methods for ServiceConnectionProvider operations.
/// EventId range: 7162-7169
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ServiceConnectionProviderLog
{
    /// <summary>
    /// Traces when a connection is about to be registered at bootstrap time.
    /// </summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Trace,
        Message = "Registering service connection '{connectionName}'")]
    public static partial IGenericMessage Registering(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a connection has been successfully registered.
    /// </summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Debug,
        Message = "Service connection '{connectionName}' registered")]
    public static partial IGenericMessage Registered(ILogger logger, string connectionName);

    /// <summary>
    /// Warns when a connection name is already registered and the registration is skipped.
    /// </summary>
    [MessageLogging(
        EventId = 41001,
        Level = LogLevel.Warning,
        Message = "Service connection '{connectionName}' is already registered — skipping duplicate registration")]
    public static partial IGenericMessage AlreadyRegistered(ILogger logger, string connectionName);

    /// <summary>
    /// Warns when a requested service connection name is not found in the registry.
    /// </summary>
    [MessageLogging(
        EventId = 31003,
        Level = LogLevel.Warning,
        Message = "Service connection '{connectionName}' not found — verify it was registered at bootstrap")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, string connectionName);

    /// <summary>
    /// Traces when a service connection is served from the instance cache.
    /// </summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Trace,
        Message = "Service connection cache hit for '{connectionName}'")]
    public static partial IGenericMessage CacheHit(ILogger logger, string connectionName);

    /// <summary>
    /// Traces when a service connection is not in the instance cache and will be resolved.
    /// </summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Trace,
        Message = "Service connection cache miss for '{connectionName}'")]
    public static partial IGenericMessage CacheMiss(ILogger logger, string connectionName);

    /// <summary>
    /// Warns when a registered connection throws during Dispose.
    /// </summary>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Warning,
        Message = "Service connection '{connectionName}' threw during disposal — exception suppressed to preserve disposal contract")]
    public static partial IGenericMessage DisposeConnectionFailed(ILogger logger, Exception exception, string connectionName);
}
