using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// MessageLogging for the row-flow engine's scripts (FDW-1443): a SQL source's read and an Execute SQL step's run.
/// EventId range: 20930-20939.
/// </summary>
[MessageLoggingTypeCode("FLOW")]
public static partial class FlowScriptLog
{
    /// <summary>Logs a script source whose read failed.</summary>
    [MessageLogging(EventId = 20930, Level = LogLevel.Error,
        Message = "Flow script source '{component}' could not read its script on data store '{dataStore}': {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string component, string dataStore, string? reason);

    /// <summary>Logs a script source whose read succeeded and returned no row set.</summary>
    [MessageLogging(EventId = 20931, Level = LogLevel.Error,
        Message = "Flow script source '{component}' read its script on data store '{dataStore}' and got no row set")]
    public static partial IGenericMessage ReadReturnedNothing(ILogger logger, string component, string dataStore);

    /// <summary>Logs a script source whose script selected more rows than it was allowed.</summary>
    [MessageLogging(EventId = 20934, Level = LogLevel.Error,
        Message = "Flow script source '{component}' selected more than the {maxRows} row(s) it may read on data store '{dataStore}'")]
    public static partial IGenericMessage TooManyRows(ILogger logger, string component, string dataStore, int maxRows);

    /// <summary>Logs an Execute SQL step whose script failed.</summary>
    [MessageLogging(EventId = 20932, Level = LogLevel.Error,
        Message = "Flow step '{step}' could not run its script on data store '{dataStore}': {reason}")]
    public static partial IGenericMessage RunFailed(ILogger logger, string step, string dataStore, string? reason);

    /// <summary>Logs a script step whose failure carried no message in a failure category, so its reason can only be the messages it did carry.</summary>
    [MessageLogging(EventId = 20936, Level = LogLevel.Error,
        Message = "Flow step '{step}' on data store '{dataStore}' failed with no coded failure message; it carried: {carried}")]
    public static partial IGenericMessage FailureCarriedNoCodedMessage(ILogger logger, string step, string dataStore, string carried);

    /// <summary>Logs an Execute SQL step whose script ran.</summary>
    [MessageLogging(EventId = 20933, Level = LogLevel.Information,
        Message = "Flow step '{step}' ran its script on data store '{dataStore}' and affected {rows} row(s)")]
    public static partial IGenericMessage RunFinished(ILogger logger, string step, string dataStore, int rows);
}
