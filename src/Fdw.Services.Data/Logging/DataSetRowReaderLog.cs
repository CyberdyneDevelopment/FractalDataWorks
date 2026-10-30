using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>MessageLogging definitions for <see cref="DataSetRowReader"/>.</summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetRowReaderLog
{
    /// <summary>A data set holds more rows than the cap.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The data set's name.</param>
    /// <param name="rowCap">The cap.</param>
    [MessageLogging(EventId = 21101, Level = LogLevel.Warning,
        Message = "Data set '{name}' holds more than {rowCap} rows, which is more than a calculation may read",
        ClientMessage = "Data set '{name}' holds more than {rowCap} rows, which is more than a calculation may read")]
    public static partial IGenericMessage OverRowCap(ILogger logger, string name, int rowCap);

    /// <summary>The rows of a data set were read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The data set's name.</param>
    /// <param name="rowCount">How many rows were read.</param>
    [MessageLogging(EventId = 11100, Level = LogLevel.Debug,
        Message = "Read {rowCount} rows of data set '{name}'")]
    public static partial IGenericMessage Read(ILogger logger, string name, int rowCount);
}
