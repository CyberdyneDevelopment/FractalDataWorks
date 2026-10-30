using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for starting a pipeline run under another execution (FDW-1381): the refusals of the run starter and the
/// StartPipeline stage. Every refusal is logged and returned in the failure result.
/// EventId range: 20801-20815.
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
    [MessageLogging(EventId = 20803, Level = LogLevel.Error,
        Message = "The execution record of pipeline '{pipeline}' started under '{parentExecutionId}' could not be created: {reason}")]
    public static partial IGenericMessage ExecutionNotCreated(ILogger logger, string pipeline, Guid parentExecutionId, string? reason);

    /// <summary>Logs a start whose start/end row could not be written.</summary>
    [MessageLogging(EventId = 20804, Level = LogLevel.Error,
        Message = "The run row of pipeline '{pipeline}' (execution '{executionId}') could not be written, so the run was not started: {reason}")]
    public static partial IGenericMessage RunRowNotWritten(ILogger logger, string pipeline, Guid executionId, string? reason);

    /// <summary>Logs a start refused because the host names no operational store.</summary>
    [MessageLogging(EventId = 20805, Level = LogLevel.Error,
        Message = "EtlPipelineTypes.OperationalConnection is not set; the host must name the operational store a run's start and end are kept in, so pipeline '{pipeline}' was not started")]
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
}
