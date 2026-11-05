using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the pipeline failure rule (FDW-3864). EventId range: 11330-11331, 60320-60323, 71320-71327.
/// </summary>
[MessageLoggingTypeCode("ETL")]
public static partial class PipelineFailureRuleLog
{
    /// <summary>Logs that a pipeline passed its failure threshold and was disabled.</summary>
    [MessageLogging(EventId = 11330, Level = LogLevel.Warning, Message = "Pipeline '{pipeline}' failed {count} runs in a row for reasons that are not transient and was disabled by rule {rule} of its thresholds")]
    public static partial IGenericMessage Disabled(ILogger logger, string pipeline, int count, int rule);

    /// <summary>Logs a failed run that recorded no failure kind, which the failure rule counts as unclassified.</summary>
    [MessageLogging(EventId = 11331, Level = LogLevel.Warning, Message = "Run {executionId} of pipeline '{pipeline}' failed but recorded no kind of failure; the failure rule counts it as unclassified")]
    public static partial IGenericMessage RunKindMissing(ILogger logger, Guid executionId, string pipeline);

    /// <summary>Logs a rule that cannot be judged because the run has no tenant.</summary>
    [MessageLogging(EventId = 60320, Level = LogLevel.Error, Message = "The failure rule of pipeline '{pipeline}' was not judged: the run belongs to no tenant")]
    public static partial IGenericMessage NoTenant(ILogger logger, string pipeline);

    /// <summary>Logs a pipeline's configuration or recent runs that could not be read.</summary>
    [MessageLogging(EventId = 71320, Level = LogLevel.Error, Message = "The failure rule of pipeline '{pipeline}' was not judged: {reason}")]
    public static partial IGenericMessage NotRead(ILogger logger, string pipeline, string reason);

    /// <summary>Logs a finished run whose row holds no completion time, so its place among the others cannot be known.</summary>
    [MessageLogging(EventId = 71323, Level = LogLevel.Error, Message = "Run {executionId} of pipeline '{pipeline}' ended as {status} but holds no completion time, so the failure rule cannot place it")]
    public static partial IGenericMessage RunTimeMissing(ILogger logger, Guid executionId, string pipeline, string status);

    /// <summary>Logs a failure-rule disable whose record could not be written on the run.</summary>
    [MessageLogging(EventId = 71324, Level = LogLevel.Error, Message = "Pipeline '{pipeline}' was disabled by its failure rule but the record on run {executionId} could not be written: {reason}")]
    public static partial IGenericMessage DisableRecordNotWritten(ILogger logger, Guid executionId, string pipeline, string reason);

    /// <summary>Logs a pipeline marked as disabled by its failure rule whose record cannot be found on the run.</summary>
    [MessageLogging(EventId = 71325, Level = LogLevel.Error, Message = "Run {executionId} holds no usable record of a failure-rule disable")]
    public static partial IGenericMessage DisableRecordMissing(ILogger logger, Guid executionId);

    /// <summary>Logs a failure rule that fired for a subject that carries no run, so the record cannot be kept.</summary>
    [MessageLogging(EventId = 60322, Level = LogLevel.Error, Message = "The failure rule of pipeline '{pipeline}' fired without a run to record it on, so the pipeline was not disabled")]
    public static partial IGenericMessage DisableWithoutRun(ILogger logger, string pipeline);

    /// <summary>Logs a firing whose counted failures were not carried in the reported values.</summary>
    [MessageLogging(EventId = 60323, Level = LogLevel.Error, Message = "The failure rule of pipeline '{pipeline}' fired but its reported values carry no readable list of counted failures, so the pipeline was not disabled")]
    public static partial IGenericMessage FailuresNotCarried(ILogger logger, string pipeline);

    /// <summary>Logs the thresholds of a pipeline that could not be judged.</summary>
    [MessageLogging(EventId = 71326, Level = LogLevel.Error, Message = "The thresholds of pipeline '{pipeline}' were not judged after its run: {reason}")]
    public static partial IGenericMessage NotJudged(ILogger logger, string pipeline, string reason);

    /// <summary>Logs a consequence of a fired threshold that failed.</summary>
    [MessageLogging(EventId = 71327, Level = LogLevel.Error, Message = "Threshold '{threshold}' rule {rule} of pipeline '{pipeline}' fired as {escalation} and the consequence failed: {reason}")]
    public static partial IGenericMessage ConsequenceFailed(ILogger logger, string threshold, int rule, string pipeline, string escalation, string reason);

    /// <summary>Logs a pipeline that passed its threshold and could not be disabled.</summary>
    [MessageLogging(EventId = 71321, Level = LogLevel.Error, Message = "Pipeline '{pipeline}' passed its failure threshold and could not be disabled: {reason}")]
    public static partial IGenericMessage DisableFailed(ILogger logger, string pipeline, string reason);

    /// <summary>Logs that the connection the run history is kept in was never named, so the history of a pipeline cannot be read.</summary>
    [MessageLogging(EventId = 71328, Level = LogLevel.Error, Message = "The operational connection is not named, so the runs of pipeline '{pipeline}' cannot be read")]
    public static partial IGenericMessage OperationalConnectionNotNamed(ILogger logger, string pipeline);
}
