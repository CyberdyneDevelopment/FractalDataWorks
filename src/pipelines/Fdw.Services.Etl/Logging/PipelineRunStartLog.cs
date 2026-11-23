using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for starting a pipeline run under another execution (FDW-1381): the refusals of the run starter and the
/// StartPipeline stage. Every refusal is logged and returned in the failure result.
/// EventId range: 20801-20818, 20822-20824, 60826, 70827, 10832-10834, 150830-150831, 90830, 70831 and the numbers on each method.
/// </summary>
[MessageLoggingTypeCode("PIPELINERUN")]
public static partial class PipelineRunStartLog
{
    /// <summary>Logs a start that names the pipeline by neither or both of its id and its name.</summary>
    [MessageLogging(EventId = 20801, Level = LogLevel.Error,
        Message = "A run started under execution '{parentExecutionId}' names the pipeline by exactly one of its id and its name; it named {named}")]
    public static partial IGenericMessage PipelineNotNamed(ILogger logger, Guid parentExecutionId, string named);

    /// <summary>Logs a pipeline that cannot be found or built for a start.</summary>
    [MessageLogging(EventId = 20802, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' cannot be started: {reason}")]
    public static partial IGenericMessage PipelineNotStartable(ILogger logger, string pipeline, string? reason);

    /// <summary>Logs a start of a pipeline that is already among the runs the start would sit under.</summary>
    [MessageLogging(EventId = 20816, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' cannot be started: the run it would be started under already descends from a run of that pipeline ('{executionId}'), so each run would start the next and never end")]
    public static partial IGenericMessage PipelineIsAncestor(ILogger logger, string pipeline, Guid executionId);

    /// <summary>Logs a start of a pipeline that requires approval to run, by a run that cannot wait for a person (FDW-1937).</summary>
    [MessageLogging(EventId = 20817, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' requires approval to run, so another pipeline or a project cannot start it: only a person's start can wait for an approval")]
    public static partial IGenericMessage PipelineRequiresApproval(ILogger logger, string pipeline);

    /// <summary>Logs a start whose execution record could not be created.</summary>
    [MessageLogging(EventId = 70803, Level = LogLevel.Error,
        Message = "The execution record of pipeline '{pipeline}' started under '{parentExecutionId}' could not be created: {reason}")]
    public static partial IGenericMessage ExecutionNotCreated(ILogger logger, string pipeline, Guid parentExecutionId, string? reason);

    /// <summary>Logs a start whose start/end row could not be written.</summary>
    [MessageLogging(EventId = 70804, Level = LogLevel.Error,
        Message = "The run row of pipeline '{pipeline}' (execution '{executionId}') could not be written, so the run was not started: {reason}")]
    public static partial IGenericMessage RunRowNotWritten(ILogger logger, string pipeline, Guid executionId, string? reason);

    /// <summary>Logs a start refused because the host names no operational store.</summary>
    [MessageLogging(EventId = 60805, Level = LogLevel.Error,
        Message = "PipelineTypes.OperationalConnection is not set; the host must name the operational store a run's start and end are kept in, so pipeline '{pipeline}' was not started")]
    public static partial IGenericMessage OperationalStoreNotNamed(ILogger logger, string pipeline);

    /// <summary>Logs a start whose request the queue did not accept: the queue is full, or a threshold on waiting runs stops new runs (FDW-3161).</summary>
    [MessageLogging(EventId = 20806, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' (execution '{executionId}') was not started: {reason}")]
    public static partial IGenericMessage RunNotTaken(ILogger logger, string pipeline, Guid executionId, string reason);

    /// <summary>Logs a start that must be awaited on a host that has no completion signaler.</summary>
    [MessageLogging(EventId = 20807, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' was to be awaited but this host has no completion signaler, so it was not started")]
    public static partial IGenericMessage CompletionNotAwaitable(ILogger logger, string pipeline);

    /// <summary>Logs a run that was started.</summary>
    [MessageLogging(EventId = 20808, Level = LogLevel.Information,
        Message = "Pipeline '{pipeline}' started as execution '{executionId}' under '{parentExecutionId}' by {triggerSource}")]
    public static partial IGenericMessage RunStarted(ILogger logger, string pipeline, Guid executionId, Guid parentExecutionId, string triggerSource);

    /// <summary>Logs a run that started at once, in the host that took the request (FDW-6141).</summary>
    [MessageLogging(EventId = 20843, Level = LogLevel.Information,
        Message = "Pipeline '{pipeline}' started at once as execution '{executionId}' by {triggerSource}")]
    public static partial IGenericMessage AtOnceRunStarted(ILogger logger, string pipeline, Guid executionId, string triggerSource);

    /// <summary>Logs a StartPipeline stage in a run that has nothing to start the pipeline under.</summary>
    [MessageLogging(EventId = 20809, Level = LogLevel.Error,
        Message = "Stage '{stageName}' of pipeline '{pipelineName}' starts another pipeline but the run states its {missing}, so there is nothing to start it under")]
    public static partial IGenericMessage StartRunUnattached(ILogger logger, string pipelineName, string stageName, string missing);

    /// <summary>Logs a StartPipeline stage on a host that cannot start a pipeline.</summary>
    [MessageLogging(EventId = 20810, Level = LogLevel.Error,
        Message = "Pipeline '{pipelineName}' has StartPipeline stage '{stageName}' but this host cannot start pipeline runs")]
    public static partial IGenericMessage StartUnavailable(ILogger logger, string pipelineName, string stageName);

    /// <summary>Logs a StartPipeline stage that is unusable: it names no pipeline or run point, or binds a parameter with no name.</summary>
    [MessageLogging(EventId = 20811, Level = LogLevel.Error,
        Message = "StartPipeline stage '{stageName}' is not usable: {reason}")]
    public static partial IGenericMessage StartStageInvalid(ILogger logger, string stageName, string reason);

    /// <summary>Logs a StartPipeline stage whose parameter value uses a variable that no run has.</summary>
    [MessageLogging(EventId = 20812, Level = LogLevel.Error,
        Message = "StartPipeline stage '{stageName}' parameter '{parameter}' uses unknown variables [{unknown}]; a run has [{reserved}]")]
    public static partial IGenericMessage StartParameterUnknownVariable(ILogger logger, string stageName, string parameter, string unknown, string reserved);

    /// <summary>Logs a StartPipeline stage that started its pipeline.</summary>
    [MessageLogging(EventId = 20813, Level = LogLevel.Information,
        Message = "StartPipeline stage '{stageName}' started pipeline '{target}' at {runPoint} as execution '{executionId}'")]
    public static partial IGenericMessage StartFired(ILogger logger, string stageName, string target, string runPoint, Guid executionId);

    /// <summary>Logs a StartPipeline stage that could not start its pipeline.</summary>
    [MessageLogging(EventId = 20814, Level = LogLevel.Error,
        Message = "StartPipeline stage '{stageName}' could not start pipeline '{target}' at {runPoint}: {reason}")]
    public static partial IGenericMessage StartFailed(ILogger logger, string stageName, string target, string runPoint, string? reason);

    /// <summary>Logs an orchestration that has no tenant to start its member pipelines under.</summary>
    [MessageLogging(EventId = 20815, Level = LogLevel.Error,
        Message = "Node '{nodeName}' cannot start its pipelines: the orchestration run states no tenant, and a started run belongs to its starter's tenant")]
    public static partial IGenericMessage OrchestrationHasNoTenant(ILogger logger, string nodeName);

    /// <summary>Logs a started run whose given parameters could not be written to its record. The run is already accepted, so it is not refused.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Warning,
        Message = "Pipeline '{pipeline}' was started as execution '{executionId}', but the parameters it was given could not be recorded: {reason}")]
    public static partial IGenericMessage RunParametersNotRecorded(ILogger logger, string pipeline, Guid executionId, string? reason);

    /// <summary>Error: checking a run's parameters needs the pipeline's name; an empty name is refused.</summary>
    [MessageLogging(EventId = 21803, Level = LogLevel.Error,
        Message = "Checking the parameters of a pipeline run needs the pipeline's name, and the name given is empty")]
    public static partial IGenericMessage CheckedPipelineNameRequired(ILogger logger);
    /// <summary>Logs a run no other run started, queued by the Queued run mode (FDW-6131).</summary>
    [MessageLogging(EventId = 10832, Level = LogLevel.Information,
        Message = "Pipeline '{pipeline}' was started as execution '{executionId}' by {triggerSource} and queued")]
    public static partial IGenericMessage RunQueued(ILogger logger, string pipeline, Guid executionId, string triggerSource);

    /// <summary>Logs a run whose execution its caller had opened, queued by the Queued run mode (FDW-6131).</summary>
    [MessageLogging(EventId = 10833, Level = LogLevel.Information,
        Message = "Pipeline '{pipeline}' (execution '{executionId}', opened by its caller) was started by {triggerSource} and queued")]
    public static partial IGenericMessage OpenedRunQueued(ILogger logger, string pipeline, Guid executionId, string triggerSource);

    /// <summary>Logs a wait on a run whose completion this host does not keep (FDW-6131).</summary>
    [MessageLogging(EventId = 20818, Level = LogLevel.Error,
        Message = "Run '{executionId}' cannot be awaited: this host has no completion signaler")]
    public static partial IGenericMessage RunCompletionNotKept(ILogger logger, Guid executionId);

    /// <summary>Logs a run whose execution its caller had opened and that the queue did not take, with the queue's code (FDW-6131).</summary>
    [MessageLogging(EventId = 10834, Level = LogLevel.Warning,
        Message = "Pipeline '{pipeline}' (execution '{executionId}', opened by its caller) was not queued: {code}")]
    public static partial IGenericMessage OpenedRunNotQueued(ILogger logger, string pipeline, Guid executionId, string code);

    /// <summary>
    /// The refusal of a run whose caller opened its execution, because the queue is full (FDW-6131). Its text is the queue's own reason, so the caller
    /// answers with exactly what the queue said; <see cref="OpenedRunNotQueued"/> logs which run it was.
    /// </summary>
    [MessageLogging(EventId = 150830, Level = LogLevel.Warning, Message = "{reason}")]
    public static partial IGenericMessage OpenedRunQueueFull(ILogger logger, string reason);

    /// <summary>The refusal of a run whose caller opened its execution, because a threshold on waiting runs stops new runs (FDW-6131); its text is the queue's reason.</summary>
    [MessageLogging(EventId = 150831, Level = LogLevel.Warning, Message = "{reason}")]
    public static partial IGenericMessage OpenedRunStopped(ILogger logger, string reason);

    /// <summary>The refusal of a run whose caller opened its execution, which the queue refused and whose refusal it could not record (FDW-6131); its text is the queue's reason.</summary>
    [MessageLogging(EventId = 90830, Level = LogLevel.Error, Message = "{reason}")]
    public static partial IGenericMessage OpenedRunRefusalNotRecorded(ILogger logger, string reason);

    /// <summary>Logs a run no other run started whose execution record could not be created (FDW-6131).</summary>
    [MessageLogging(EventId = 70831, Level = LogLevel.Error,
        Message = "The execution record of pipeline '{pipeline}' could not be created: {reason}")]
    public static partial IGenericMessage RootExecutionNotCreated(ILogger logger, string pipeline, string? reason);

    /// <summary>Logs a run whose record cannot say where the run came from, because the run mode or the host's name is not stated (FDW-6519): no row is written.</summary>
    [MessageLogging(EventId = 60842, Level = LogLevel.Error,
        Message = "A run's record must state {missing}, and it is not stated, so the run is refused and no record is written")]
    public static partial IGenericMessage RunOriginIncomplete(ILogger logger, string missing);

    /// <summary>Logs a start under another run when the host does not set how long a chain of runs may be (FDW-6135): no limit is assumed.</summary>
    [MessageLogging(EventId = 20822, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' was started under another run, and the setting {setting} is not set to a positive number, so the length of the chain cannot be judged and the start is refused; no limit is assumed")]
    public static partial IGenericMessage PipelineChainLimitNotSet(ILogger logger, string pipeline, string setting);

    /// <summary>Logs a start that would make a chain of runs longer than the setting allows (FDW-6135).</summary>
    [MessageLogging(EventId = 20823, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' would be run number {depth} of a chain of runs started one by another, and at most {maximum} are allowed, so the start is refused")]
    public static partial IGenericMessage PipelineChainLimitReached(ILogger logger, string pipeline, int depth, int maximum);

    /// <summary>Logs a start of a pipeline a variable named that the run's tenant and person are not offered (FDW-6135).</summary>
    [MessageLogging(EventId = 20824, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' is not offered to tenant {tenantId} and the person and roles of the run, so it is not started")]
    public static partial IGenericMessage PipelineNotOffered(ILogger logger, string pipeline, Guid tenantId);

    /// <summary>Logs a start that needs an offer on a host that has no way to read offers (FDW-6135): the start is refused, and nothing is assumed offered.</summary>
    [MessageLogging(EventId = 60826, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' was named by a variable, so it must be offered to the run, and this host reads no offers, so the start is refused")]
    public static partial IGenericMessage PipelineOffersUnavailable(ILogger logger, string pipeline);

    /// <summary>Logs the read of the offers of a pipeline that failed (FDW-6135): the start is refused.</summary>
    [MessageLogging(EventId = 70827, Level = LogLevel.Error,
        Message = "The offers of pipeline '{pipeline}' could not be read: {reason}, so the start is refused")]
    public static partial IGenericMessage PipelineOffersNotRead(ILogger logger, string pipeline, string? reason);
}
