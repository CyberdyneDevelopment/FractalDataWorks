using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Components.Logging;

/// <summary>
/// MessageLogging for QualityDashboardProvider operations.
/// EventId range: 4420-4424
/// </summary>
[MessageLoggingTypeCode("COMPONENTS13")]
public static partial class QualityDashboardProviderLog
{
    /// <summary>
    /// Logs that the quality dashboard data is being loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "QualityDashboardProvider: Loading dashboard data")]
    public static partial IGenericMessage LoadStarted(ILogger logger);

    /// <summary>
    /// Logs that the quality dashboard data was loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "QualityDashboardProvider: Dashboard data loaded")]
    public static partial IGenericMessage LoadCompleted(ILogger logger);

    /// <summary>
    /// Logs that loading the quality dashboard data failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "QualityDashboardProvider: Failed to load dashboard data")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while loading the quality dashboard data.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while loading the dashboard data.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Warning,
        Message = "QualityDashboardProvider: Exception loading dashboard data")]
    public static partial IGenericMessage LoadException(ILogger logger, Exception exception);
}
