using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Analytics.Clients.Logging;

/// <summary>
/// MessageLogging for the HTTP-backed health monitor client.
/// EventId range: 4600-4603
/// </summary>
[MessageLoggingTypeCode("ANALYTICSCLIENTS")]
public static partial class HealthMonitorClientLog
{
    /// <summary>Logs a system health GET failure.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Health monitor system GET failed with status {statusCode}")]
    public static partial IGenericMessage GetSystemHealthFailed(ILogger logger, int statusCode);

    /// <summary>Logs a service health GET failure.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Health monitor service GET failed for '{serviceName}' with status {statusCode}")]
    public static partial IGenericMessage GetServiceHealthFailed(ILogger logger, string serviceName, int statusCode);

    /// <summary>Logs a service health history GET failure.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Health monitor history GET failed for '{serviceName}' with status {statusCode}")]
    public static partial IGenericMessage GetHealthHistoryFailed(ILogger logger, string serviceName, int statusCode);

    /// <summary>Logs a service throughput GET failure.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Health monitor throughput GET failed for '{serviceName}' with status {statusCode}")]
    public static partial IGenericMessage GetThroughputFailed(ILogger logger, string serviceName, int statusCode);
}
