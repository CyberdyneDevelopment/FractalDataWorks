using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>MessageLogging for listing the data option types and their options (FDW-5665).</summary>
/// <remarks>EventIds follow the result categories (first digit of a five-digit id): 1 progress.</remarks>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class DataOptionTypeEndpointLog
{
    /// <summary>Logs that the data option types are being listed.</summary>
    [MessageLogging(EventId = 11220, Level = LogLevel.Trace, Message = "Listing data option types")]
    public static partial IGenericMessage ListingDataOptionTypes(ILogger logger);

    /// <summary>Logs how many data option types were listed.</summary>
    [MessageLogging(EventId = 11221, Level = LogLevel.Debug, Message = "Listed {count} data option types")]
    public static partial IGenericMessage ListedDataOptionTypes(ILogger logger, int count);
}
