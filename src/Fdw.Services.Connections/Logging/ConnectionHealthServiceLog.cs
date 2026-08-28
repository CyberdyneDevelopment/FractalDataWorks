using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for <see cref="ConnectionHealthService"/> operations.
/// EventId range: 12100-12110.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionHealthServiceLog
{
    /// <summary>Logs that a health check record is about to be persisted.</summary>
    [MessageLogging(EventId = 12100, Level = LogLevel.Trace, Message = "Recording health check for connection '{connectionName}' ({connectionId}): healthy={isHealthy}")]
    public static partial IGenericMessage RecordingHealthCheck(ILogger logger, string connectionName, Guid connectionId, bool isHealthy);

    /// <summary>Logs that a health check record was persisted successfully, for a Healthy result.</summary>
    [MessageLogging(EventId = 12101, Level = LogLevel.Debug, Message = "Health check recorded for connection '{connectionName}': healthy={isHealthy}, responseTimeMs={responseTimeMs}")]
    public static partial IGenericMessage HealthCheckRecorded(ILogger logger, string connectionName, bool isHealthy, int? responseTimeMs);

    /// <summary>Logs that a health check record was persisted successfully, for an Unhealthy result.</summary>
    [MessageLogging(EventId = 12108, Level = LogLevel.Error, Message = "Health check recorded for connection '{connectionName}': healthy={isHealthy}, responseTimeMs={responseTimeMs}")]
    public static partial IGenericMessage HealthCheckRecordedUnhealthy(ILogger logger, string connectionName, bool isHealthy, int? responseTimeMs);

    /// <summary>Logs that persisting a health check record failed.</summary>
    [MessageLogging(EventId = 12102, Level = LogLevel.Error, Message = "Failed to persist health check for connection '{connectionName}': {error}")]
    public static partial IGenericMessage RecordHealthCheckFailed(ILogger logger, string connectionName, string error);

    /// <summary>Logs that health check history is about to be queried.</summary>
    [MessageLogging(EventId = 12103, Level = LogLevel.Trace, Message = "Querying health check history for connection {connectionId} (count={count})")]
    public static partial IGenericMessage QueryingHistory(ILogger logger, Guid connectionId, int count);

    /// <summary>Logs that health check history was retrieved successfully.</summary>
    [MessageLogging(EventId = 12104, Level = LogLevel.Debug, Message = "Retrieved {resultCount} health check record(s) for connection {connectionId}")]
    public static partial IGenericMessage HistoryRetrieved(ILogger logger, Guid connectionId, int resultCount);

    /// <summary>Logs that querying health check history failed.</summary>
    [MessageLogging(EventId = 12105, Level = LogLevel.Error, Message = "Failed to query health check history for connection {connectionId}: {error}")]
    public static partial IGenericMessage QueryHistoryFailed(ILogger logger, Guid connectionId, string error);

    /// <summary>Logs that the health-check insert command returned a failure result. The command's own messages travel on the chained result, so no error text is duplicated here.</summary>
    [MessageLogging(EventId = 12106, Level = LogLevel.Error, Message = "Failed to persist health check for connection '{connectionName}'")]
    public static partial IGenericMessage RecordHealthCheckCommandFailed(ILogger logger, string connectionName);

    /// <summary>Logs that the health-history query command returned a failure result. The command's own messages travel on the chained result, so no error text is duplicated here.</summary>
    [MessageLogging(EventId = 12107, Level = LogLevel.Error, Message = "Failed to query health check history for connection {connectionId}")]
    public static partial IGenericMessage QueryHistoryCommandFailed(ILogger logger, Guid connectionId);
}
