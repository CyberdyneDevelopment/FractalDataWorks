using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>MessageLogging for the stand-in strategy endpoints (set/get/preview).</summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class StandInEndpointLog
{
    /// <summary>Logs the start of setting a field's stand-in.</summary>
    [MessageLogging(EventId = 11027, Level = LogLevel.Trace, Message = "Setting stand-in for '{dataSetName}.{fieldName}' to strategy '{strategy}'")]
    public static partial IGenericMessage SettingStandIn(ILogger logger, string dataSetName, string fieldName, string strategy);

    /// <summary>Logs that a field's stand-in was set.</summary>
    [MessageLogging(EventId = 11028, Level = LogLevel.Information, Message = "Set stand-in for '{dataSetName}.{fieldName}' to strategy '{strategy}'")]
    public static partial IGenericMessage StandInSet(ILogger logger, string dataSetName, string fieldName, string strategy);

    /// <summary>Logs that a named field was not found on a data set.</summary>
    [MessageLogging(EventId = 31005, Level = LogLevel.Warning, Message = "Field '{fieldName}' not found in data set '{dataSetName}'")]
    public static partial IGenericMessage FieldNotFound(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs the start of reading a field's active stand-in.</summary>
    [MessageLogging(EventId = 11029, Level = LogLevel.Trace, Message = "Getting stand-in for '{dataSetName}.{fieldName}'")]
    public static partial IGenericMessage GettingStandIn(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs the start of a stand-in preview.</summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Trace, Message = "Previewing strategy '{strategy}' for '{dataSetName}.{fieldName}', count={count}")]
    public static partial IGenericMessage PreviewingStandIn(ILogger logger, string dataSetName, string fieldName, string strategy, int count);

    /// <summary>Logs a gateway failure writing a stand-in.</summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Warning, Message = "Failed to write stand-in for '{dataSetName}.{fieldName}'")]
    public static partial IGenericMessage StandInWriteFailed(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs a gateway failure reading a stand-in.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Warning, Message = "Failed to read stand-in for '{dataSetName}.{fieldName}'")]
    public static partial IGenericMessage StandInReadFailed(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs a request to clear a field's stand-in.</summary>
    [MessageLogging(EventId = 11035, Level = LogLevel.Trace, Message = "Clearing stand-in for '{dataSetName}.{fieldName}'")]
    public static partial IGenericMessage ClearingStandIn(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs a field's stand-in cleared.</summary>
    [MessageLogging(EventId = 11036, Level = LogLevel.Information, Message = "Cleared the stand-in for '{dataSetName}.{fieldName}'")]
    public static partial IGenericMessage StandInCleared(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs a clear of a field that has no stand-in; nothing was changed.</summary>
    [MessageLogging(EventId = 31009, Level = LogLevel.Warning, Message = "'{dataSetName}.{fieldName}' has no stand-in to clear")]
    public static partial IGenericMessage NoStandInToClear(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs the start of reading every field's stand-in on a data set.</summary>
    [MessageLogging(EventId = 11037, Level = LogLevel.Trace, Message = "Getting the stand-ins of every field of '{dataSetName}'")]
    public static partial IGenericMessage GettingStandIns(ILogger logger, string dataSetName);

    /// <summary>Logs the result of reading every field's stand-in on a data set.</summary>
    [MessageLogging(EventId = 11038, Level = LogLevel.Debug, Message = "Read the stand-ins of '{dataSetName}': {fieldCount} field(s), {standInCount} with a stand-in")]
    public static partial IGenericMessage StandInsRead(ILogger logger, string dataSetName, int fieldCount, int standInCount);

    /// <summary>Logs a data set that could not be found for a stand-in read.</summary>
    [MessageLogging(EventId = 31010, Level = LogLevel.Warning, Message = "Data set '{dataSetName}' not found for a stand-in read")]
    public static partial IGenericMessage DataSetNotFoundForStandIns(ILogger logger, string dataSetName);

    /// <summary>Logs a stand-in row for which none of the strategy tables has a current row.</summary>
    [MessageLogging(EventId = 71013, Level = LogLevel.Error, Message = "The stand-in of field '{fieldName}' ({standInId}) has no current row in any strategy table")]
    public static partial IGenericMessage StandInHasNoStrategy(ILogger logger, string fieldName, System.Guid standInId);
}
