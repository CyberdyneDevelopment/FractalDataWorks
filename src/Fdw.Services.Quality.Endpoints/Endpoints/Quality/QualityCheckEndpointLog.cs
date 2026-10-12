using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// MessageLogging definitions for the quality check execute endpoints.
/// </summary>
[MessageLoggingTypeCode("QUALITYENDPOINTS")]
public static partial class QualityCheckEndpointLog
{
    /// <summary>Logs that a check cannot run because nothing names the data set whose store it would read.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "{subject} names no data set to check, so there is no store to read; the store comes from the data set's own configuration and is never guessed")]
    public static partial IGenericMessage NamesNoDataSet(ILogger logger, string subject);

    /// <summary>Logs that a check did not run because the rows of its data set were not read.</summary>
    // Why Information: the gateway already logged why the read failed, at that failure's own level. This line only
    // records that no rule ran and that the read's own failure is what the caller is answered with.
    [MessageLogging(EventId = 11015, Level = LogLevel.Information,
        Message = "Quality check over data set '{dataSetName}' did not run: its rows were not read, and that read's failure is the answer")]
    public static partial IGenericMessage RowsNotRead(ILogger logger, string dataSetName);
}
