using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>MessageLogging for the read of which pipelines write each data set (FDW-4488).</summary>
[MessageLoggingTypeCode("ETL")]
public static partial class DataSetWriterLog
{
    /// <summary>Logs writers left out because they name their data set by a placeholder.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="writerCount">How many writes name a data set by a <c>{{Parameter}}</c>.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Information and not a failure: a pipeline that picks its data set when it runs has no data set to name before a run; the other pipelines still read.
    [MessageLogging(
        EventId = 61158,
        Level = LogLevel.Information,
        Message = "{writerCount} pipeline write(s) name their data set by a placeholder and are left out of the data set writers")]
    public static partial IGenericMessage PlaceholderWriters(ILogger logger, int writerCount);
}
