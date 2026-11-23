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

    /// <summary>Logs when a schedule list is asked to sort by a column that does not exist.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning, Message = "Schedules cannot be sorted by '{sortBy}'; the columns are name, pipelinename and nextruntime")]
    public static partial IGenericMessage UnknownSortField(ILogger logger, string sortBy);

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

    /// <summary>Logs a request to have a schedule's runs act as the caller, from a caller who does not hold the permission for it (FDW-6144).</summary>
    [MessageLogging(EventId = 101003, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}': the caller may not have its runs act as them; it needs the permission {permission}",
        ClientMessage = "You may not have a schedule's runs act as you")]
    public static partial IGenericMessage RunAsNotPermitted(ILogger logger, string scheduleName, string permission);

    /// <summary>Logs a request whose grant ends at no stated time (FDW-6144).</summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}': the request to run as the caller states no end for the grant; it must state when it ends",
        ClientMessage = "State when the grant ends")]
    public static partial IGenericMessage RunAsExpiryMissing(ILogger logger, string scheduleName);

    /// <summary>Logs a request whose grant allows permissions the caller does not hold (FDW-6144).</summary>
    [MessageLogging(EventId = 101005, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}': the grant would allow {notHeld}, which the caller does not hold; a grant allows only what the person who gives it holds",
        ClientMessage = "A grant can allow only what you hold yourself")]
    public static partial IGenericMessage RunAsExceedsPerson(ILogger logger, string scheduleName, string notHeld);

    /// <summary>Logs a request whose grant allows less than the schedule's run roles carry (FDW-6144).</summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}': the grant does not allow {notAllowed}, which the schedule's run roles carry",
        ClientMessage = "The grant must allow everything the run roles carry")]
    public static partial IGenericMessage RunAsNotCovering(ILogger logger, string scheduleName, string notAllowed);

    /// <summary>Logs a request to stop running as the caller on a schedule that is not (FDW-6144).</summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}': the request asks to stop its runs acting as a person, but they act as none",
        ClientMessage = "This schedule's runs do not act as a person")]
    public static partial IGenericMessage RunAsNothingToRemove(ILogger logger, string scheduleName);

    /// <summary>Logs a Remove request that also states a grant (FDW-6144).</summary>
    [MessageLogging(EventId = 21008, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}': the request both removes the grant and states one",
        ClientMessage = "Either remove the grant or state a new one, not both")]
    public static partial IGenericMessage RunAsRemoveAndStated(ILogger logger, string scheduleName);
}
