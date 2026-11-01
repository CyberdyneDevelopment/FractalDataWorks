using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>MessageLogging for the data set field history endpoint (API-174).</summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class DataSetFieldHistoryEndpointLog
{
    /// <summary>Logs the start of a field history read.</summary>
    [MessageLogging(EventId = 11580, Level = LogLevel.Trace, Message = "Reading the history of '{dataSetName}.{fieldName}'")]
    public static partial IGenericMessage ReadingHistory(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs how many versions a field has.</summary>
    [MessageLogging(EventId = 11581, Level = LogLevel.Debug, Message = "'{dataSetName}.{fieldName}' has {versionCount} stored version(s)")]
    public static partial IGenericMessage HistoryRead(ILogger logger, string dataSetName, string fieldName, int versionCount);

    /// <summary>Logs that a named field was not found on a data set.</summary>
    [MessageLogging(EventId = 31580, Level = LogLevel.Warning, Message = "Field '{fieldName}' not found in data set '{dataSetName}'")]
    public static partial IGenericMessage FieldNotFound(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs that a field's stored versions could not be read.</summary>
    [MessageLogging(EventId = 71580, Level = LogLevel.Error, Message = "The stored versions of '{dataSetName}.{fieldName}' could not be read")]
    public static partial IGenericMessage HistoryReadFailed(ILogger logger, string dataSetName, string fieldName);
}
