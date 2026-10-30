using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// MessageLogging for cancelling, pausing and resuming a pipeline run (FDW-2950).
/// EventId ranges: 10994-10997 (a run was told, or ended as cancelled), 40994-40996 (the request does not fit the run's state, 409).
/// </summary>
[MessageLoggingTypeCode("PIPELINERUN")]
public static partial class PipelineRunControlLog
{
    /// <summary>Logs a run told to stop.</summary>
    [MessageLogging(EventId = 10994, Level = LogLevel.Information, Message = "Pipeline run {executionId} was told to stop")]
    public static partial IGenericMessage CancelRequested(ILogger logger, Guid executionId);

    /// <summary>Logs a run told to hold.</summary>
    [MessageLogging(EventId = 10995, Level = LogLevel.Information, Message = "Pipeline run {executionId} was told to pause")]
    public static partial IGenericMessage PauseRequested(ILogger logger, Guid executionId);

    /// <summary>Logs a paused run released.</summary>
    [MessageLogging(EventId = 10996, Level = LogLevel.Information, Message = "Pipeline run {executionId} was told to resume")]
    public static partial IGenericMessage ResumeRequested(ILogger logger, Guid executionId);

    /// <summary>Logs a run that ended as cancelled because a person cancelled it.</summary>
    [MessageLogging(EventId = 10997, Level = LogLevel.Information, Message = "Pipeline run {executionId} of pipeline '{pipelineName}' ended as cancelled")]
    public static partial IGenericMessage RunCancelled(ILogger logger, Guid executionId, string pipelineName);

    /// <summary>Logs a request for a run this host does not hold.</summary>
    [MessageLogging(EventId = 40994, Level = LogLevel.Warning,
        Message = "Pipeline run {executionId} cannot be told to {action}: this host holds no such run",
        ClientMessage = "The run cannot be told to {action}: it has not started, it has finished, or it is held by another host")]
    public static partial IGenericMessage RunNotHeld(ILogger logger, Guid executionId, string action);

    /// <summary>Logs a pause of a run that is already paused.</summary>
    [MessageLogging(EventId = 40995, Level = LogLevel.Warning,
        Message = "Pipeline run {executionId} is already paused",
        ClientMessage = "The run is already paused")]
    public static partial IGenericMessage AlreadyPaused(ILogger logger, Guid executionId);

    /// <summary>Logs a resume of a run that is not paused.</summary>
    [MessageLogging(EventId = 40996, Level = LogLevel.Warning,
        Message = "Pipeline run {executionId} is not paused",
        ClientMessage = "The run is not paused")]
    public static partial IGenericMessage NotPaused(ILogger logger, Guid executionId);
}
