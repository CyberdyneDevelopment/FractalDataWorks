using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints.Logging;

/// <summary>
/// MessageLogging for the transformer chains a pipeline request states on the field mappings of its Map stages (FDW-1120):
/// what is refused about a chain before the pipeline is saved. Each message is both logged and handed back as the reason of
/// the problem the caller reads.
/// EventId range: 20311-20329.
/// </summary>
[MessageLoggingTypeCode("STAGECHAIN")]
public static partial class StageChainLog
{
    /// <summary>Logs a chain step that names its Transformation and also gives its id.</summary>
    [MessageLogging(EventId = 20311, Level = LogLevel.Warning,
        Message = "Step {ordinal} of the chain of field mapping '{mappingName}' names a Transformation and also gives its id; state one of them")]
    public static partial IGenericMessage StepNamesBoth(ILogger logger, string mappingName, int ordinal);

    /// <summary>Logs a chain step that names no Transformation and gives no id.</summary>
    [MessageLogging(EventId = 20312, Level = LogLevel.Warning,
        Message = "Step {ordinal} of the chain of field mapping '{mappingName}' names no Transformation and gives no id; state one of them")]
    public static partial IGenericMessage StepNamesNeither(ILogger logger, string mappingName, int ordinal);

    /// <summary>Logs a chain whose ordinals are not 1 to the number of steps, each once.</summary>
    [MessageLogging(EventId = 20313, Level = LogLevel.Warning,
        Message = "The chain of field mapping '{mappingName}' has {stepCount} step(s), so its ordinals must run from 1 without gaps, each used once")]
    public static partial IGenericMessage OrdinalsNotConsecutive(ILogger logger, string mappingName, int stepCount);

    /// <summary>Logs a field mapping that states both a transform expression and a chain.</summary>
    [MessageLogging(EventId = 20314, Level = LogLevel.Warning,
        Message = "Field mapping '{mappingName}' states both transformExpression and a chain of transforms; state one of them")]
    public static partial IGenericMessage ExpressionAndChain(ILogger logger, string mappingName);

    /// <summary>Logs a chain step that names a Transformation that does not exist.</summary>
    [MessageLogging(EventId = 20315, Level = LogLevel.Warning,
        Message = "Step {ordinal} of the chain of field mapping '{mappingName}' names Transformation '{transformationName}', which does not exist")]
    public static partial IGenericMessage TransformationNotFound(ILogger logger, string mappingName, int ordinal, string transformationName);

    /// <summary>Logs a chain step that gives the id of a Transformation that does not exist.</summary>
    [MessageLogging(EventId = 20316, Level = LogLevel.Warning,
        Message = "Step {ordinal} of the chain of field mapping '{mappingName}' gives Transformation id {transformationId}, which does not exist")]
    public static partial IGenericMessage TransformationIdNotFound(ILogger logger, string mappingName, int ordinal, Guid transformationId);

    /// <summary>Logs a chain step whose Transformation does not transform a single field's value.</summary>
    [MessageLogging(EventId = 20317, Level = LogLevel.Warning,
        Message = "Step {ordinal} of the chain of field mapping '{mappingName}' uses Transformation '{transformationName}', a {implementation}, which does not transform a single field's value")]
    public static partial IGenericMessage TransformationNotFieldLevel(ILogger logger, string mappingName, int ordinal, string transformationName, string implementation);

    /// <summary>Logs that the chains of a request were looked up.</summary>
    [MessageLogging(EventId = 20318, Level = LogLevel.Debug,
        Message = "Looked up the Transformations of {stepCount} chain step(s) for pipeline '{pipelineName}': {problemCount} problem(s)")]
    public static partial IGenericMessage ChainsLookedUp(ILogger logger, string pipelineName, int stepCount, int problemCount);
}
