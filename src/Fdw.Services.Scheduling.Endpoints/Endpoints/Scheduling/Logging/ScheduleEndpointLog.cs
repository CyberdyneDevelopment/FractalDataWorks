using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints.Logging;

/// <summary>
/// MessageLogging for Schedule endpoint base operations.
/// EventId range: 7251
/// </summary>
/// <remarks>
/// Why: Relocated from 7140 to avoid collision with ConnectionProviderLogger (7140-7163 trace diagnostics).
/// The 7248-7260 range is reserved for configuration endpoint logs.
/// </remarks>
[MessageLoggingTypeCode("ENDPOINTS10")]
public static partial class ScheduleEndpointLog
{
    /// <summary>Logs when a modification is rejected because the schedule is a system configuration.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "Rejected modification of system schedule '{scheduleName}' — system configurations are read-only")]
    public static partial IGenericMessage SystemScheduleReadOnly(ILogger logger, string scheduleName);

    /// <summary>Logs when a schedule delete operation fails.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to delete schedule '{scheduleName}'")]
    public static partial IGenericMessage DeleteFailed(ILogger logger, string scheduleName);

    /// <summary>Logs when a schedule configuration is not found during an endpoint operation.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Schedule '{scheduleName}' not found")]
    public static partial IGenericMessage ScheduleNotFound(ILogger logger, string scheduleName);

    /// <summary>Logs when the schedule types list endpoint is called.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace, Message = "Listing schedule types from ScheduleTypes TypeCollection")]
    public static partial IGenericMessage ListingScheduleTypes(ILogger logger);

    /// <summary>Logs when the schedule types list returns successfully.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Listed {count} schedule types")]
    public static partial IGenericMessage ListedScheduleTypes(ILogger logger, int count);
}
