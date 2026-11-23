using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Logging;

/// <summary>
/// Messages of the pipeline layer's run contract (FDW-6131): how a start is handed to the runner of its pipeline type's run mode, and each reason a start, a wait or a
/// follow is refused, each with its own number.
/// EventId range: 10830-10831, 10835, 20830-20832, 30830-30831, 60830-60832, 100830.
/// </summary>
[MessageLoggingTypeCode("PIPELINERUNS")]
public static partial class PipelineRunsLog
{
    /// <summary>Logs a start handed to the runner of its pipeline type's run mode.</summary>
    [MessageLogging(EventId = 10830, Level = LogLevel.Debug,
        Message = "Pipeline '{pipeline}' is of pipeline type '{pipelineType}'; its run is handed to the '{runMode}' runner")]
    public static partial IGenericMessage RunHandedToRunner(ILogger logger, string pipeline, string pipelineType, string runMode);

    /// <summary>Logs a stop asked of a run, with its reason.</summary>
    [MessageLogging(EventId = 10831, Level = LogLevel.Information,
        Message = "Run '{executionId}' is asked to stop: {reason}")]
    public static partial IGenericMessage RunStopAsked(ILogger logger, Guid executionId, string reason);

    /// <summary>Logs the person a run acts as, filled from its caller (FDW-6137).</summary>
    [MessageLogging(EventId = 10835, Level = LogLevel.Debug,
        Message = "Pipeline '{pipeline}' is started as person '{personId}', {source}")]
    public static partial IGenericMessage RunActsAsPerson(ILogger logger, string pipeline, Guid personId, string source);

    /// <summary>Logs a start of a pipeline whose type is for a person, that acts as none (FDW-6137).</summary>
    [MessageLogging(EventId = 20832, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' is not started: it is of the type '{type}', which runs for a person, and the start acts as none",
        ClientMessage = "That pipeline runs for a person, and the caller is not one")]
    public static partial IGenericMessage RunPersonMissing(ILogger logger, string pipeline, string type);

    /// <summary>Logs a start that names another person than its caller (FDW-6137).</summary>
    [MessageLogging(EventId = 100830, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' is not started: the start names person '{named}', and the run acts as {actsAs}")]
    public static partial IGenericMessage RunPersonMismatch(ILogger logger, string pipeline, Guid named, string actsAs);

    /// <summary>Logs a start that names the pipeline by neither or both of its id and its name.</summary>
    [MessageLogging(EventId = 20830, Level = LogLevel.Error,
        Message = "A pipeline run names the pipeline by exactly one of its id and its name; it named {named}")]
    public static partial IGenericMessage PipelineNotNamed(ILogger logger, string named);

    /// <summary>Logs a wait on a run that was not started here to be awaited.</summary>
    [MessageLogging(EventId = 20831, Level = LogLevel.Error,
        Message = "Run '{executionId}' cannot be awaited: it was not started here with its completion kept")]
    public static partial IGenericMessage RunNotAwaitable(ILogger logger, Guid executionId);

    /// <summary>Logs a start of a pipeline the caller cannot read.</summary>
    [MessageLogging(EventId = 30830, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' cannot be started: {reason}")]
    public static partial IGenericMessage PipelineNotFound(ILogger logger, string pipeline, string? reason);

    /// <summary>Logs a follow of a run whose execution record cannot be read.</summary>
    [MessageLogging(EventId = 30831, Level = LogLevel.Error,
        Message = "Run '{executionId}' cannot be followed: {reason}")]
    public static partial IGenericMessage RunNotReadable(ILogger logger, Guid executionId, string? reason);

    /// <summary>Logs a start of a pipeline whose implementation no <see cref="PipelineTypes"/> option names (FDW-6244).</summary>
    [MessageLogging(EventId = 60830, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' is implemented as '{implementation}', which no pipeline type names, so it cannot be started")]
    public static partial IGenericMessage PipelineTypeUnknown(ILogger logger, string pipeline, string implementation);

    /// <summary>Logs a start of a pipeline whose pipeline type's run mode no runner in this host carries out.</summary>
    [MessageLogging(EventId = 60831, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' is of pipeline type '{pipelineType}', whose runs are carried out '{runMode}', and this host has no runner for that mode")]
    public static partial IGenericMessage PipelineRunModeUnsupported(ILogger logger, string pipeline, string pipelineType, string runMode);

    /// <summary>Logs a pipeline type that names a run mode no run mode option declares (FDW-6244).</summary>
    [MessageLogging(EventId = 60832, Level = LogLevel.Error,
        Message = "Pipeline '{pipeline}' is of pipeline type '{pipelineType}', which names the run mode '{runMode}', and no run mode of that name is declared")]
    public static partial IGenericMessage PipelineTypeRunModeUnknown(ILogger logger, string pipeline, string pipelineType, string runMode);

    /// <summary>Logs a start that requires a run mode no run mode option declares (FDW-6701); the pipeline type's own run mode is not used in its place.</summary>
    [MessageLogging(EventId = 60851, Level = LogLevel.Error,
        Message = "The start of pipeline '{pipeline}' requires the run mode '{runMode}', and no run mode of that name is declared, so it is refused; the run mode of the pipeline's type is not used in its place")]
    public static partial IGenericMessage RequiredRunModeUnknown(ILogger logger, string pipeline, string runMode);

    /// <summary>Logs a start that requires a run mode no runner in this host carries out (FDW-6701); the pipeline type's own runner is not used in its place.</summary>
    [MessageLogging(EventId = 60852, Level = LogLevel.Error,
        Message = "The start of pipeline '{pipeline}' requires the run mode '{runMode}', and this host has no runner for that mode, so it is refused; the runner of the pipeline's type is not used in its place")]
    public static partial IGenericMessage RequiredRunModeUnsupported(ILogger logger, string pipeline, string runMode);
}
