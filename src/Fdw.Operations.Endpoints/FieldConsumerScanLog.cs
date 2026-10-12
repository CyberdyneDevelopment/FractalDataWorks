using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>MessageLogging for <see cref="FieldConsumerScanner"/> (API-173).</summary>
[MessageLoggingTypeCode("ENDPOINTS3")]
internal static partial class FieldConsumerScanLog
{
    /// <summary>Logs how many consumers were found for a data set's fields.</summary>
    [MessageLogging(EventId = 11590, Level = LogLevel.Debug, Message = "Found {consumerCount} consumer(s) across {fieldCount} field(s) of data set '{dataSetName}'")]
    internal static partial IGenericMessage Scanned(ILogger logger, string dataSetName, int fieldCount, int consumerCount);

    /// <summary>Logs that consumers could not be determined.</summary>
    [MessageLogging(EventId = 71590, Level = LogLevel.Error, Message = "The consumers of data set '{dataSetName}' fields could not be determined")]
    internal static partial IGenericMessage ScanFailed(ILogger logger, string dataSetName);
}
