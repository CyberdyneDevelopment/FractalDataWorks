using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// MessageLogging for the pipeline run scheduler (FDW-3085, FDW-1511): runs that wait for their turn, a run handing its place back
/// while it waits for a pipeline it started, a limit setting that is missing, and a pipeline that states how many of its runs may run at once
/// (FDW-3117), and a run held until a set time (FDW-3129).
/// EventId range: 20988-20993, 22110-22115.
/// </summary>
[MessageLoggingTypeCode("PIPELINERUN")]
public static partial class PipelineRunSchedulerLog
{
    /// <summary>Logs a run handing its place back while it waits.</summary>
    [MessageLogging(EventId = 20990, Level = LogLevel.Debug, Message = "A pipeline run is waiting for a pipeline it started and hands its place back")]
    public static partial IGenericMessage PlaceHandedBack(ILogger logger);

    /// <summary>Logs a run taking its place again.</summary>
    [MessageLogging(EventId = 20991, Level = LogLevel.Debug, Message = "A pipeline run that waited takes its place again")]
    public static partial IGenericMessage PlaceTakenAgain(ILogger logger);

    /// <summary>Logs a run that is queued behind others.</summary>
    [MessageLogging(EventId = 20992, Level = LogLevel.Debug, Message = "A pipeline run waits for its turn: {running} running, {waiting} waiting")]
    public static partial IGenericMessage RunWaits(ILogger logger, int running, int waiting);

    /// <summary>Logs a run that got its place.</summary>
    [MessageLogging(EventId = 20993, Level = LogLevel.Debug, Message = "A pipeline run starts: {running} running, {waiting} waiting")]
    public static partial IGenericMessage RunStarts(ILogger logger, int running, int waiting);

    /// <summary>Logs a run that must run alone, and why.</summary>
    [MessageLogging(EventId = 20989, Level = LogLevel.Information, Message = "Pipeline '{pipelineName}' runs alone because {reason}")]
    public static partial IGenericMessage RunMustBeAlone(ILogger logger, string pipelineName, string reason);

    /// <summary>Logs that a limit setting on pipeline runs is missing or not positive.</summary>
    [MessageLogging(EventId = 20988, Level = LogLevel.Error, Message = "The setting '{settingName}' that limits how many pipeline runs are in flight is missing or not positive, so the run cannot start")]
    public static partial IGenericMessage LimitSettingMissing(ILogger logger, string settingName);

    /// <summary>Logs a run refused because its pipeline already has as many runs as it may have at once.</summary>
    [MessageLogging(EventId = 22110, Level = LogLevel.Warning, Message = "Pipeline '{pipelineName}' already has {limit} runs active, its limit, and its overflow behavior is Refuse, so the run is refused")]
    public static partial IGenericMessage RunRefused(ILogger logger, string pipelineName, int limit);

    /// <summary>Logs a pipeline that does not state a usable number of parallel runs or overflow behavior.</summary>
    [MessageLogging(EventId = 22111, Level = LogLevel.Error, Message = "Pipeline '{pipelineName}' does not state a usable {property}: {reason}, so the run cannot start")]
    public static partial IGenericMessage ParallelismInvalid(ILogger logger, string pipelineName, string property, string reason);

    /// <summary>Logs a run held because its pipeline is at its limit and holds such a run until a set time.</summary>
    [MessageLogging(EventId = 22112, Level = LogLevel.Information, Message = "Pipeline '{pipelineName}' already has {limit} runs active, its limit, and its overflow behavior is DelayedStart, so the run is held until {heldUntil} ({delay})")]
    public static partial IGenericMessage RunHeld(ILogger logger, string pipelineName, int limit, System.DateTimeOffset heldUntil, string delay);

    /// <summary>Logs a held run joining the queue.</summary>
    [MessageLogging(EventId = 22113, Level = LogLevel.Information, Message = "A held run of pipeline '{pipelineName}' reached its time and joins the queue")]
    public static partial IGenericMessage HeldRunDue(ILogger logger, string pipelineName);

    /// <summary>Logs a run held before a host restart that is held again until the time it was held until (FDW-3216).</summary>
    [MessageLogging(EventId = 22115, Level = LogLevel.Information, Message = "A run of pipeline '{pipelineName}' that was held before the host restarted is held again until {heldUntil}")]
    public static partial IGenericMessage RunHeldAgain(ILogger logger, string pipelineName, System.DateTimeOffset heldUntil);

    /// <summary>Logs a trigger dropped because the host already holds as many triggers as its setting allows.</summary>
    [MessageLogging(EventId = 22114, Level = LogLevel.Warning, Message = "The ETL host already holds {capacity} pipeline triggers, the number the setting '{settingName}' gives, so the trigger of pipeline '{pipelineName}' is dropped")]
    public static partial IGenericMessage TriggerDropped(ILogger logger, int capacity, string settingName, string pipelineName);
}
