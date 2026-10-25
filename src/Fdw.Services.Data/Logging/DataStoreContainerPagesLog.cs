using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for reading the containers of all data stores one page at a time (FDW-2311).
/// EventId ranges: 11099 (done), 91095 (a row without a column the page reads, 500).
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataStoreContainerPagesLog
{
    /// <summary>Logs that a page of containers was read.</summary>
    [MessageLogging(EventId = 11099, Level = LogLevel.Debug, Message = "A page of {count} data store containers was read (skip {skip}, take {take})")]
    public static partial IGenericMessage Paged(ILogger logger, int count, int skip, int take);

    /// <summary>Logs a container row without a column the page reads.</summary>
    [MessageLogging(EventId = 91095, Level = LogLevel.Error, Message = "A data container row lacks the column '{column}' that a page of containers reads")]
    public static partial IGenericMessage ColumnMissing(ILogger logger, string column);
}
