using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the Union stage (FDW-1120 phase 4): a run that cannot append the second data set, and what an append
/// did. Every refusal is logged and returned in the failure result.
/// EventId range: 20500-20599.
/// </summary>
[MessageLoggingTypeCode("TRANSFORM")]
public static partial class UnionLog
{
    /// <summary>Logs that a Union stage was asked to run on one record at a time, which it cannot do.</summary>
    [MessageLogging(EventId = 20500, Level = LogLevel.Error,
        Message = "Union '{name}' appends a second data set after the whole source, which a run that handles one record at a time (a Streaming pipeline) cannot do")]
    public static partial IGenericMessage UnionNeedsABatchRun(ILogger logger, string name);

    /// <summary>Logs that the run has no data gateway to read the union source through.</summary>
    [MessageLogging(EventId = 20501, Level = LogLevel.Error,
        Message = "Union '{name}' cannot read data set '{dataSet}': the run has no data gateway")]
    public static partial IGenericMessage UnionHasNoGateway(ILogger logger, string name, string dataSet);

    /// <summary>Logs that the union source could not be read.</summary>
    [MessageLogging(EventId = 20502, Level = LogLevel.Error,
        Message = "Union '{name}' could not read data set '{dataSet}': {reason}")]
    public static partial IGenericMessage UnionSourceUnreadable(ILogger logger, string name, string dataSet, string reason);

    /// <summary>Logs a row of the union source that lacks a field the stage's mapping reads.</summary>
    [MessageLogging(EventId = 20503, Level = LogLevel.Error,
        Message = "Union '{name}' read a row of data set '{dataSet}' without the field '{fieldName}'")]
    public static partial IGenericMessage UnionRowLacksField(ILogger logger, string name, string dataSet, string fieldName);

    /// <summary>Logs that a Union stage appended the rows of its source.</summary>
    [MessageLogging(EventId = 20504, Level = LogLevel.Debug,
        Message = "Union '{name}' kept {mainCount} record(s) and appended {unionCount} from data set '{dataSet}'")]
    public static partial IGenericMessage UnionApplied(ILogger logger, string name, int mainCount, int unionCount, string dataSet);
}
