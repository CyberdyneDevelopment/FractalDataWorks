using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>Messages of the reporting of a run's component figures to the threshold evaluator (FDW-3865).</summary>
[MessageLoggingTypeCode("RUNTHRESHOLD")]
public static partial class RunThresholdLog
{
    /// <summary>The host runs pipelines but registered no threshold evaluator, so its runs could not be judged.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 23004, Level = LogLevel.Critical,
        Message = "The host runs graph pipelines but registered no IThresholdEvaluator, so a run could not be judged against thresholds; the host does not start")]
    public static partial IGenericMessage EvaluatorNotRegistered(ILogger logger);

    /// <summary>A threshold stopped a run.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="reason">Why.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 23002, Level = LogLevel.Warning, Message = "Pipeline run {executionId} is being stopped by a threshold: {reason}")]
    public static partial IGenericMessage RunStoppedByThreshold(ILogger logger, Guid executionId, string reason);

    /// <summary>A component has no identifier in the run's configuration, so its figures cannot be attached to its thresholds.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipeline">The pipeline's name.</param>
    /// <param name="component">The component's name.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 23003, Level = LogLevel.Error,
        Message = "The component '{component}' of the pipeline '{pipeline}' has no identifier in the run's configuration, so its figures are not reported to its thresholds")]
    public static partial IGenericMessage ComponentHasNoId(ILogger logger, string pipeline, string component);

    /// <summary>The evaluator could not judge a component's figures.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="subject">The component's subject name.</param>
    /// <param name="reason">Why.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 23001, Level = LogLevel.Error, Message = "The figures of {subject} were not judged against its thresholds: {reason}")]
    public static partial IGenericMessage ReportFailed(ILogger logger, string subject, string reason);
}
