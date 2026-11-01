using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for a data query that filters or sorts on a calculated field in memory (FDW-1045). The type code is the one the message
/// was written under before the read moved into this assembly.
/// </summary>
[MessageLoggingTypeCode("RESTENDPOINTS")]
public static partial class CalculatedQueryLog
{
    /// <summary>Logs that a data query read rows to filter or sort on a calculated field in memory (FDW-1045).</summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="dataSet">The data set that was queried.</param>
    /// <param name="rowCount">The rows the store handed over.</param>
    /// <param name="rowCap">The most rows such a query may read.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Information,
        Message = "Data set {dataSet}: read {rowCount} rows (cap {rowCap}) to filter or sort on a calculated field in memory")]
    public static partial IGenericMessage CalculatedQueryRead(ILogger logger, string dataSet, int rowCount, int rowCap);
}
