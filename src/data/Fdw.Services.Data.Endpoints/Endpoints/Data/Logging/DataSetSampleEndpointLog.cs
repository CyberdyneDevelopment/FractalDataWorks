using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>MessageLogging for the data set sample endpoints (list/upsert).</summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class DataSetSampleEndpointLog
{
    /// <summary>Logs the start of listing a data set's samples.</summary>
    [MessageLogging(EventId = 11031, Level = LogLevel.Trace, Message = "Listing samples for data set '{dataSetName}'")]
    public static partial IGenericMessage ListingSamples(ILogger logger, string dataSetName);

    /// <summary>Logs how many samples were found.</summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Debug, Message = "Listed {count} samples for data set '{dataSetName}'")]
    public static partial IGenericMessage SamplesListed(ILogger logger, string dataSetName, int count);

    /// <summary>Logs the start of upserting a sample.</summary>
    [MessageLogging(EventId = 11033, Level = LogLevel.Trace, Message = "Upserting sample '{sampleName}' for data set '{dataSetName}' with {rowCount} rows")]
    public static partial IGenericMessage UpsertingSample(ILogger logger, string dataSetName, string sampleName, int rowCount);

    /// <summary>Logs that a sample was upserted.</summary>
    [MessageLogging(EventId = 11034, Level = LogLevel.Information, Message = "Upserted sample '{sampleName}' for data set '{dataSetName}' with {rowCount} rows")]
    public static partial IGenericMessage SampleUpserted(ILogger logger, string dataSetName, string sampleName, int rowCount);

    /// <summary>Logs that a sample row named an unknown field.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning, Message = "Sample row for data set '{dataSetName}' references unknown field '{fieldName}', ignoring")]
    public static partial IGenericMessage SampleUnknownField(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs a gateway failure writing a sample.</summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Warning, Message = "Failed to write sample '{sampleName}' for data set '{dataSetName}'")]
    public static partial IGenericMessage SampleWriteFailed(ILogger logger, string dataSetName, string sampleName);

    /// <summary>Logs a gateway failure listing samples.</summary>
    [MessageLogging(EventId = 71012, Level = LogLevel.Warning, Message = "Failed to list samples for data set '{dataSetName}'")]
    public static partial IGenericMessage SamplesListFailed(ILogger logger, string dataSetName);
}
