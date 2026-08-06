using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Execution;

/// <summary>
/// MessageLogging methods for PredicatePushdownAnalyzer.
/// EventId range: 5201-5210
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class PredicatePushdownLog
{
    /// <summary>Logs that no filter was provided for predicate pushdown analysis.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "No filter provided for predicate pushdown analysis")]
    public static partial IGenericMessage NoFilterProvided(ILogger logger);

    /// <summary>Logs the start of filter decomposition across sources.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Decomposing filter for DataSet '{dataSetName}' across {sourceCount} sources")]
    public static partial IGenericMessage DecomposingFilter(ILogger logger, string dataSetName, int sourceCount);

    /// <summary>Logs that a condition was routed to a specific source.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Routed condition on field '{fieldName}' to source '{sourceName}'")]
    public static partial IGenericMessage ConditionRouted(ILogger logger, string fieldName, string sourceName);

    /// <summary>Logs that a field was not found in any source mappings.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Field '{fieldName}' not found in any source mappings for DataSet '{dataSetName}'")]
    public static partial IGenericMessage FieldNotMapped(ILogger logger, string fieldName, string dataSetName);

    /// <summary>Logs that a source was not found in the DataSet.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "Source '{sourceName}' not found in DataSet '{dataSetName}'")]
    public static partial IGenericMessage SourceNotFound(ILogger logger, string sourceName, string dataSetName);

    /// <summary>Logs that a source does not support predicate pushdown.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Source '{sourceName}' does not support predicate pushdown - filter will be applied post-join")]
    public static partial IGenericMessage PushdownNotSupported(ILogger logger, string sourceName);

    /// <summary>Logs that an optimized filter was generated for a source.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Generated optimized filter for source '{sourceName}' with {conditionCount} conditions")]
    public static partial IGenericMessage FilterGenerated(ILogger logger, string sourceName, int conditionCount);

    /// <summary>Logs the completion of filter decomposition.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Filter decomposition complete - generated {filterCount} source-specific filters")]
    public static partial IGenericMessage DecompositionComplete(ILogger logger, int filterCount);

    /// <summary>Logs that filter decomposition failed.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Filter decomposition failed: {error}")]
    public static partial IGenericMessage DecompositionFailed(ILogger logger, string error);

    /// <summary>Logs that filter translation failed.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "Filter translation failed: {error}")]
    public static partial IGenericMessage TranslationFailed(ILogger logger, string error);
}
