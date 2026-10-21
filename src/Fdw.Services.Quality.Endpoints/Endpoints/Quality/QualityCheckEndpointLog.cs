using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// MessageLogging definitions for the quality check execute endpoints.
/// </summary>
[MessageLoggingTypeCode("QUALITYENDPOINTS")]
public static partial class QualityCheckEndpointLog
{
    /// <summary>Logs that a check cannot run because nothing names the data set whose store it would read.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "{subject} names no data set to check, so there is no store to read; the store comes from the data set's own configuration and is never guessed")]
    public static partial IGenericMessage NamesNoDataSet(ILogger logger, string subject);

    /// <summary>Logs that a check did not run because the rows of its data set were not read.</summary>
    // Why Information: the gateway already logged why the read failed, at that failure's own level. This line only
    // records that no rule ran and that the read's own failure is what the caller is answered with.
    [MessageLogging(EventId = 11015, Level = LogLevel.Information,
        Message = "Quality check over data set '{dataSetName}' did not run: its rows were not read, and that read's failure is the answer")]
    public static partial IGenericMessage RowsNotRead(ILogger logger, string dataSetName);

    /// <summary>Logs that a check cannot be recorded because the caller has no active tenant to record it under.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning,
        Message = "A quality check is recorded under the caller's active tenant and the caller has none, so the check was not run")]
    public static partial IGenericMessage NoActiveTenant(ILogger logger);

    /// <summary>Logs that the host names no operational store, so no verdict can be kept or read.</summary>
    [MessageLogging(EventId = 20003, Level = LogLevel.Error,
        Message = "QualityServiceTypes.OperationalConnection is not set; the host must name the operational store the quality executions are kept in, and none is guessed")]
    public static partial IGenericMessage ExecutionStoreNotNamed(ILogger logger);

    /// <summary>Logs that a check ran but its verdict was not kept.</summary>
    [MessageLogging(EventId = 20004, Level = LogLevel.Error,
        Message = "Quality rule '{ruleId}' ran but its verdict was not recorded: {reason}")]
    public static partial IGenericMessage ExecutionNotRecorded(ILogger logger, Guid ruleId, string? reason);

    /// <summary>Logs that recorded executions were not read.</summary>
    [MessageLogging(EventId = 20005, Level = LogLevel.Error,
        Message = "Executions of '{subject}' were not read: {reason}")]
    public static partial IGenericMessage ExecutionsNotRead(ILogger logger, string subject, string? reason);

    /// <summary>Logs a request for a number of executions outside the range one read answers.</summary>
    [MessageLogging(EventId = 20006, Level = LogLevel.Warning,
        Message = "Take must be given and be from 1 to {maximum}; {take} was asked for")]
    public static partial IGenericMessage TakeOutOfRange(ILogger logger, int maximum, int take);

    /// <summary>Logs that a verdict was recorded.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Information,
        Message = "Quality rule '{ruleId}' over data set '{dataSetName}' recorded as {outcome}")]
    public static partial IGenericMessage ExecutionRecorded(ILogger logger, Guid ruleId, string dataSetName, string outcome);
}
