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
}
