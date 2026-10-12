using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Logging;

/// <summary>
/// MessageLogging for DataSet schema operations.
/// EventId range: 7070-7089
/// </summary>
[MessageLoggingTypeCode("PIPELINES")]
internal static partial class DataSetSchemaLog
{
    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Getting schema for DataSet {dataSetId}")]
    public static partial IGenericMessage GetSchemaStarted(ILogger logger, Guid dataSetId);

    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Got schema for DataSet {dataSetId}: {count} fields")]
    public static partial IGenericMessage GetSchemaSucceeded(ILogger logger, Guid dataSetId, int count);

    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to get schema for DataSet {dataSetId}: {error}")]
    public static partial IGenericMessage GetSchemaFailed(ILogger logger, Guid dataSetId, string error);

    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Failed to get schema for DataSet {dataSetId}")]
    public static partial IGenericMessage GetSchemaFailed(ILogger logger, Exception ex, Guid dataSetId);

    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Saving schema for DataSet {dataSetId}: {count} fields")]
    public static partial IGenericMessage SaveSchemaStarted(ILogger logger, Guid dataSetId, int count);

    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Saved schema for DataSet {dataSetId}")]
    public static partial IGenericMessage SaveSchemaSucceeded(ILogger logger, Guid dataSetId);

    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to save schema for DataSet {dataSetId}: {error}")]
    public static partial IGenericMessage SaveSchemaFailed(ILogger logger, Guid dataSetId, string error);

    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Failed to save schema for DataSet {dataSetId}")]
    public static partial IGenericMessage SaveSchemaFailed(ILogger logger, Exception ex, Guid dataSetId);

    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "Checking conformance: physical {physicalId} against abstract {abstractId}")]
    public static partial IGenericMessage ConformanceCheckStarted(ILogger logger, Guid physicalId, Guid abstractId);

    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Conformance check passed: physical {physicalId} conforms to abstract {abstractId}")]
    public static partial IGenericMessage ConformanceCheckPassed(ILogger logger, Guid physicalId, Guid abstractId);

    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Conformance check failed: field '{fieldName}' not found or type mismatch in physical DataSet {physicalId}")]
    public static partial IGenericMessage ConformanceCheckFailed(ILogger logger, Guid physicalId, string fieldName);
}
