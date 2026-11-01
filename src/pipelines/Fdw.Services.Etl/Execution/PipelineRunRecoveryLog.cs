using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// MessageLogging for keeping the requests of accepted pipeline runs and taking them up again after a restart (FDW-3216).
/// EventId range: 22116-22136.
/// </summary>
[MessageLoggingTypeCode("PIPELINERUN")]
public static partial class PipelineRunRecoveryLog
{
    /// <summary>Logs a request that could not be written, so the run is not accepted.</summary>
    [MessageLogging(EventId = 22116, Level = LogLevel.Error,
        Message = "The request of pipeline run {executionId} could not be written, so the run cannot be taken up again after a restart and is not accepted: {reason}")]
    public static partial IGenericMessage RequestNotWritten(ILogger logger, Guid executionId, string? reason);

    /// <summary>Logs a run parameter whose value cannot be kept with the request.</summary>
    [MessageLogging(EventId = 22117, Level = LogLevel.Error,
        Message = "Run parameter '{parameter}' holds a value of type {type}, which the record of a run's request cannot keep: {reason}")]
    public static partial IGenericMessage ValueNotKept(ILogger logger, string parameter, string type, string reason);

    /// <summary>Logs a recorded request that cannot be read.</summary>
    [MessageLogging(EventId = 22118, Level = LogLevel.Error,
        Message = "The recorded request of pipeline run {executionId} cannot be read: {reason}")]
    public static partial IGenericMessage RequestUnreadable(ILogger logger, Guid executionId, string reason);

    /// <summary>Logs the runs a host takes up again as it starts.</summary>
    [MessageLogging(EventId = 22119, Level = LogLevel.Information,
        Message = "The host takes up {recovered} pipeline runs that were accepted and not started, {held} of them held, and {approvals} runs that were waiting for approval; {withoutRequest} unstarted records have no request that can be taken up")]
    public static partial IGenericMessage RunsRecovered(ILogger logger, int recovered, int held, int approvals, int withoutRequest);

    /// <summary>Logs one run taken up again.</summary>
    [MessageLogging(EventId = 22120, Level = LogLevel.Information,
        Message = "Pipeline run {executionId} of '{pipeline}' is taken up again after a restart, {state}")]
    public static partial IGenericMessage RunTakenUp(ILogger logger, Guid executionId, string pipeline, string state);

    /// <summary>Logs that the runs to take up could not be read, so the host starts without them.</summary>
    [MessageLogging(EventId = 22121, Level = LogLevel.Error,
        Message = "The pipeline runs that were accepted and not started could not be read, so the host starts without them: {reason}")]
    public static partial IGenericMessage RecoveryFailed(ILogger logger, string? reason);

    /// <summary>Logs a stored request whose run is no longer unstarted, removed so it is never started.</summary>
    [MessageLogging(EventId = 22122, Level = LogLevel.Information,
        Message = "The stored request of pipeline run {executionId} is removed: the run is {state}, not unstarted")]
    public static partial IGenericMessage StaleRequestRemoved(ILogger logger, Guid executionId, string state);

    /// <summary>Logs a record of a run that cannot be taken up and could not be closed.</summary>
    [MessageLogging(EventId = 22123, Level = LogLevel.Error,
        Message = "Pipeline run {executionId} cannot be taken up again and its record could not be closed: {reason}")]
    public static partial IGenericMessage RecordNotClosed(ILogger logger, Guid executionId, string? reason);

    /// <summary>Logs a hold that could not be recorded, so a restart will not keep the run's time.</summary>
    [MessageLogging(EventId = 22124, Level = LogLevel.Error,
        Message = "The hold of pipeline run {executionId} until {heldUntil} could not be recorded, so a restart will not keep its time: {reason}")]
    public static partial IGenericMessage HoldNotRecorded(ILogger logger, Guid executionId, DateTimeOffset heldUntil, string? reason);

    /// <summary>Logs a run that was waiting for approval put back where it waits.</summary>
    [MessageLogging(EventId = 22125, Level = LogLevel.Information,
        Message = "Pipeline run {executionId} of '{pipeline}' that was waiting for approval waits for it again after a restart")]
    public static partial IGenericMessage ApprovalWaitsAgain(ILogger logger, Guid executionId, string pipeline);

    /// <summary>Logs a run with no tenant, whose request cannot be kept.</summary>
    [MessageLogging(EventId = 22126, Level = LogLevel.Error,
        Message = "Pipeline run {executionId} of '{pipeline}' has no tenant, so its request cannot be kept and the run is not accepted")]
    public static partial IGenericMessage RunHasNoTenant(ILogger logger, Guid executionId, string pipeline);

    /// <summary>Logs that the host does not name the operational store the requests are kept in.</summary>
    [MessageLogging(EventId = 22127, Level = LogLevel.Error,
        Message = "The host does not name its operational store, so the requests of pipeline runs cannot be kept or read")]
    public static partial IGenericMessage StoreNotNamed(ILogger logger);

    /// <summary>Logs the record of a stored request that cannot be read, left as it is.</summary>
    [MessageLogging(EventId = 22128, Level = LogLevel.Error,
        Message = "The execution record of pipeline run {executionId}, whose request is stored, cannot be read, so the run is not taken up and its request is kept: {reason}")]
    public static partial IGenericMessage RunRecordUnreadable(ILogger logger, Guid executionId, string? reason);

    /// <summary>Logs a run left running because the host is stopping (FDW-3236).</summary>
    [MessageLogging(EventId = 22130, Level = LogLevel.Information,
        Message = "Pipeline run {executionId} of '{pipeline}' is left running because the host is stopping; the host starts it again when it starts")]
    public static partial IGenericMessage RunStoppedWithHost(ILogger logger, Guid executionId, string pipeline);

    /// <summary>Logs a metrics row of a cut-off or restarted run that could not be written.</summary>
    [MessageLogging(EventId = 22136, Level = LogLevel.Warning,
        Message = "The metrics row of pipeline run {executionId} could not be written: {reason}")]
    public static partial IGenericMessage MetricsNotWritten(ILogger logger, Guid executionId, string? reason);

    /// <summary>Logs the runs found running when the host starts.</summary>
    [MessageLogging(EventId = 22135, Level = LogLevel.Information,
        Message = "{runs} pipeline runs were running when the host stopped")]
    public static partial IGenericMessage RunsWereRunning(ILogger logger, int runs);

    /// <summary>Logs a run that was running when the host stopped, closed and started again as a new run.</summary>
    [MessageLogging(EventId = 22131, Level = LogLevel.Information,
        Message = "Pipeline run {executionId} of '{pipeline}' was running when the host stopped; it is closed as failed and started again as run {newExecutionId}, restart {restart} of {limit}")]
    public static partial IGenericMessage RunStartedAgain(ILogger logger, Guid executionId, string pipeline, Guid newExecutionId, int restart, int limit);

    /// <summary>Logs a run that was running when the host stopped and is not started again.</summary>
    [MessageLogging(EventId = 22132, Level = LogLevel.Warning,
        Message = "Pipeline run {executionId} of '{pipeline}' was running when the host stopped and is not started again: {reason}")]
    public static partial IGenericMessage RunNotStartedAgain(ILogger logger, Guid executionId, string pipeline, string reason);

    /// <summary>Logs the setting that limits how often a cut-off run is started again when it is not set.</summary>
    [MessageLogging(EventId = 22133, Level = LogLevel.Error,
        Message = "The setting '{setting}' that limits how many times the host starts a cut-off pipeline run again is not set or is negative, so no cut-off run is started again")]
    public static partial IGenericMessage RestartLimitMissing(ILogger logger, string setting);

    /// <summary>Logs a cut-off run that could not be started again.</summary>
    [MessageLogging(EventId = 22134, Level = LogLevel.Error,
        Message = "Pipeline run {executionId} was running when the host stopped and could not be started again: {reason}")]
    public static partial IGenericMessage StartAgainFailed(ILogger logger, Guid executionId, string? reason);

    /// <summary>Logs a stored request that could not be removed.</summary>
    [MessageLogging(EventId = 22129, Level = LogLevel.Warning,
        Message = "The stored request of pipeline run {executionId} could not be removed: {reason}")]
    public static partial IGenericMessage RequestNotRemoved(ILogger logger, Guid executionId, string? reason);
}
