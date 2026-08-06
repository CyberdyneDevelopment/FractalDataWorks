using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Web.Analytics.Components.Health.Logging;

/// <summary>
/// MessageLogging methods for HealthDashboardProvider operations.
/// EventId range: 8930-8939
/// </summary>
[MessageLoggingTypeCode("COMPONENTS19")]
public static partial class HealthDashboardProviderLog
{
    /// <summary>Logs when health dashboard data load starts.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "[HealthDashboardProvider] Loading health dashboard data")]
    public static partial IGenericMessage LoadStarted(
        ILogger logger);

    /// <summary>Logs when health dashboard data load completes.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "[HealthDashboardProvider] Dashboard loaded - overall status: {overallStatus}, {serviceCount} service(s)")]
    public static partial IGenericMessage LoadCompleted(
        ILogger logger,
        string overallStatus,
        int serviceCount);

    /// <summary>Logs when health dashboard data load fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Warning,
        Message = "[HealthDashboardProvider] Failed to load health dashboard data")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger);

    /// <summary>Logs when health dashboard data load fails with exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning,
        Message = "[HealthDashboardProvider] Exception loading health dashboard data")]
    public static partial IGenericMessage LoadException(
        ILogger logger,
        Exception exception);
}
