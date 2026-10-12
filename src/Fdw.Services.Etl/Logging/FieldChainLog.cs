using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the transformer chains of the field mappings of a pipeline's Map stages (FDW-1120): resolving the
/// named Transformations a mapping applies when the run starts. Every refusal is logged and returned in the failure result.
/// EventId range: 20301-20399.
/// </summary>
[MessageLoggingTypeCode("FIELDCHAIN")]
public static partial class FieldChainLog
{
    /// <summary>Logs that chains cannot be resolved because the run has no Transformation provider.</summary>
    [MessageLogging(EventId = 20301, Level = LogLevel.Error,
        Message = "Stage '{stageName}' field mapping '{mappingName}' states a chain of named Transformations, but the host gave the run no Transformation provider to resolve them")]
    public static partial IGenericMessage TransformationProviderMissing(ILogger logger, string stageName, string mappingName);

    /// <summary>Logs a chain step whose Transformation no longer resolves.</summary>
    [MessageLogging(EventId = 20302, Level = LogLevel.Error,
        Message = "Stage '{stageName}' field mapping '{mappingName}' applies Transformation {transformationId}, which no longer resolves")]
    public static partial IGenericMessage TransformationMissing(ILogger logger, string stageName, string mappingName, Guid transformationId);

    /// <summary>Logs a chain step whose Transformation does not work on a single field's value.</summary>
    [MessageLogging(EventId = 20303, Level = LogLevel.Error,
        Message = "Stage '{stageName}' field mapping '{mappingName}' applies Transformation '{transformationName}', a {implementation}, which does not transform a single field's value")]
    public static partial IGenericMessage TransformationNotFieldLevel(ILogger logger, string stageName, string mappingName, string transformationName, string implementation);

    /// <summary>Logs a chain step whose Transformation names an operation no option implements.</summary>
    [MessageLogging(EventId = 20304, Level = LogLevel.Error,
        Message = "Stage '{stageName}' field mapping '{mappingName}' applies Transformation '{transformationName}', whose operation '{operationName}' is not registered")]
    public static partial IGenericMessage TransformationOperationUnknown(ILogger logger, string stageName, string mappingName, string transformationName, string operationName);

    /// <summary>Logs that a run resolved the chains of its field mappings.</summary>
    [MessageLogging(EventId = 20305, Level = LogLevel.Debug,
        Message = "Resolved {mappingCount} transformer chain(s) of {stepCount} step(s) for the field mappings of the run")]
    public static partial IGenericMessage ChainsResolved(ILogger logger, int mappingCount, int stepCount);
}
