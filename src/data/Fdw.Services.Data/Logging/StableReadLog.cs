using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>MessageLogging for <see cref="StableRead"/> (FDW-4911): which way each stable read is made.</summary>
[MessageLoggingTypeCode("DATA")]
public static partial class StableReadLog
{
    /// <summary>A container is read through its connection's cursor.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="target">What is read.</param>
    [MessageLogging(EventId = 11320, Level = LogLevel.Trace,
        Message = "Stable read of {target}: through the connection's cursor")]
    public static partial IGenericMessage ReadingThroughCursor(ILogger logger, string target);

    /// <summary>A container is read in pages in the order its store keeps.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="target">What is read.</param>
    /// <param name="order">The order the store keeps, as its connection states it.</param>
    /// <param name="pageSize">The rows a page asks for.</param>
    [MessageLogging(EventId = 11321, Level = LogLevel.Trace,
        Message = "Stable read of {target}: in pages of {pageSize} in the store's own order ({order})")]
    public static partial IGenericMessage ReadingInStorePages(ILogger logger, string target, string order, int pageSize);

    /// <summary>A container or a data set is read in keyset pages.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="target">What is read.</param>
    /// <param name="keys">The key fields the pages follow.</param>
    /// <param name="pageSize">The rows a page asks for.</param>
    [MessageLogging(EventId = 11322, Level = LogLevel.Trace,
        Message = "Stable read of {target}: in keyset pages of {pageSize} by {keys}")]
    public static partial IGenericMessage ReadingInKeysetPages(ILogger logger, string target, string keys, int pageSize);

    /// <summary>A data set whose kind does not page in the store is read once, unpaged.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSet">The data set.</param>
    /// <param name="rowCap">The most rows the read may take.</param>
    [MessageLogging(EventId = 11323, Level = LogLevel.Trace,
        Message = "Stable read of data set '{dataSet}': once, unpaged, up to {rowCap} rows, because its kind does not page in the store")]
    public static partial IGenericMessage ReadingUnpaged(ILogger logger, string dataSet, int rowCap);
}
