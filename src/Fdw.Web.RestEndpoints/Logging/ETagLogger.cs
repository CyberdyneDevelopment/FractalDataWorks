using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Logging;

/// <summary>
/// MessageLogging for ETag caching operations.
/// EventId range: 8030-8034
/// </summary>
[MessageLoggingTypeCode("RESTENDPOINTS")]
public static partial class ETagLogger
{
    /// <summary>
    /// Logs when an ETag query starts for a container.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="containerName">The container being queried for ETag.</param>
    /// <param name="connectionName">The connection name used for the query.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Querying ETag for container '{containerName}' on connection '{connectionName}'")]
    public static partial IGenericMessage ETagQueryStarted(
        ILogger logger,
        string containerName,
        string connectionName);

    /// <summary>
    /// Logs when an ETag query fails (non-success result).
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="containerName">The container that was queried.</param>
    /// <param name="connectionName">The connection name used for the query.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Error,
        Message = "ETag query returned non-success for container '{containerName}' on connection '{connectionName}'")]
    public static partial IGenericMessage ETagQueryFailed(
        ILogger logger,
        string containerName,
        string connectionName);

    /// <summary>
    /// Logs when no RowId is found in the container (empty table or no RowId column).
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="containerName">The container that was queried.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "No RowId found in container '{containerName}', skipping ETag")]
    public static partial IGenericMessage ETagNoRowId(
        ILogger logger,
        string containerName);

    /// <summary>
    /// Logs when an ETag is successfully computed.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="containerName">The container the ETag was computed for.</param>
    /// <param name="etag">The computed ETag value.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "ETag computed for container '{containerName}': {etag}")]
    public static partial IGenericMessage ETagComputed(
        ILogger logger,
        string containerName,
        string etag);

    /// <summary>
    /// Logs when an ETag query throws an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="containerName">The container that was queried.</param>
    /// <param name="connectionName">The connection name used for the query.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "ETag query error for container '{containerName}' on connection '{connectionName}'")]
    public static partial IGenericMessage ETagQueryError(
        ILogger logger,
        Exception exception,
        string containerName,
        string connectionName);
}
