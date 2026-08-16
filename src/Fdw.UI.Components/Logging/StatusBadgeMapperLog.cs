using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Components.Logging;

/// <summary>
/// MessageLogging for <see cref="Fdw.UI.Components.Services.StatusBadgeMapper"/> operations.
/// EventId range: 11009-11014.
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS3")]
public static partial class StatusBadgeMapperLog
{
    /// <summary>Logs entry to <c>FromHealth</c>.</summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Trace,
        Message = "Mapping health state {isHealthy} to a status badge")]
    public static partial IGenericMessage MappingHealth(ILogger logger, bool isHealthy);

    /// <summary>Logs the badge resolved by <c>FromHealth</c>.</summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Health state {isHealthy} mapped to badge '{label}'")]
    public static partial IGenericMessage MappedHealth(ILogger logger, bool isHealthy, string label);

    /// <summary>Logs entry to <c>FromPipelineStatus</c>.</summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Trace,
        Message = "Mapping pipeline status '{status}' to a status badge")]
    public static partial IGenericMessage MappingPipelineStatus(ILogger logger, string? status);

    /// <summary>Logs the badge resolved by <c>FromPipelineStatus</c>.</summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Debug,
        Message = "Pipeline status '{status}' mapped to badge '{label}'")]
    public static partial IGenericMessage MappedPipelineStatus(ILogger logger, string? status, string label);

    /// <summary>Logs entry to <c>FromScheduleState</c>.</summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Trace,
        Message = "Mapping schedule enabled state {isEnabled} to a status badge")]
    public static partial IGenericMessage MappingScheduleState(ILogger logger, bool isEnabled);

    /// <summary>Logs the badge resolved by <c>FromScheduleState</c>.</summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Debug,
        Message = "Schedule enabled state {isEnabled} mapped to badge '{label}'")]
    public static partial IGenericMessage MappedScheduleState(ILogger logger, bool isEnabled, string label);
}
