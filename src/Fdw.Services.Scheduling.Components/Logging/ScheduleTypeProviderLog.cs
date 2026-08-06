using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Components.Logging;

/// <summary>
/// MessageLogging methods for ScheduleProvider schedule type loading operations.
/// EventId range: 4280-4289
/// </summary>
[MessageLoggingTypeCode("COMPONENTS14")]
public static partial class ScheduleTypeProviderLog
{
    /// <summary>Logs when schedule types are being loaded from the API.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "ScheduleProvider: Loading schedule types from configuration API")]
    public static partial IGenericMessage LoadingScheduleTypes(ILogger logger);

    /// <summary>Logs when schedule types have been loaded successfully.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "ScheduleProvider: Loaded {count} schedule types")]
    public static partial IGenericMessage LoadedScheduleTypes(ILogger logger, int count);

    /// <summary>Logs when schedule type loading fails (non-exception).</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "ScheduleProvider: Failed to load schedule types from configuration API")]
    public static partial IGenericMessage LoadScheduleTypesFailed(ILogger logger);

    /// <summary>Logs when schedule type loading fails with an exception.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "ScheduleProvider: Exception loading schedule types")]
    public static partial IGenericMessage LoadScheduleTypesException(ILogger logger, Exception exception);
}
