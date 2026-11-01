using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>MessageLogging for listing the format types (FDW-1315).</summary>
/// <remarks>EventIds follow the result categories (first digit of a five-digit id): 1 progress.</remarks>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class FormatTypeEndpointLog
{
    /// <summary>Logs that the format types are being listed.</summary>
    [MessageLogging(EventId = 11210, Level = LogLevel.Trace, Message = "Listing format types")]
    public static partial IGenericMessage ListingFormatTypes(ILogger logger);

    /// <summary>Logs how many format types were listed.</summary>
    [MessageLogging(EventId = 11211, Level = LogLevel.Debug, Message = "Listed {count} format types")]
    public static partial IGenericMessage ListedFormatTypes(ILogger logger, int count);
}
