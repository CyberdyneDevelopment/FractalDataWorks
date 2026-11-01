using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for the data-set read that /data and a calculation's DataSet input share (FDW-1075). The type code is the one the /data
/// routes answered under before the read moved into this assembly, so a caller sees the same codes.
/// </summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class DataQueryLog
{
    /// <summary>Refuses a view whose stored filter cannot be read, instead of reading its rows unfiltered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="viewName">The view asked for.</param>
    /// <param name="reason">Why the stored filter cannot be read.</param>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "The view '{viewName}' cannot be read: {reason}")]
    public static partial IGenericMessage ViewFilterInvalid(ILogger logger, string viewName, string reason);
}
