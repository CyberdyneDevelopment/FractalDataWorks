using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// MessageLogging for the Read data options step and for a list variable rendered as text (FDW-5347).
/// EventId range: 21010-21016, and 71010 for a read that failed (the catalog's retryable IO band, as FlowScriptLog's read failure).
/// </summary>
[MessageLoggingTypeCode("FLOW")]
public static partial class FlowDataOptionsLog
{
    /// <summary>Logs a list variable that cannot be written as quoted text because it is empty or an item is null or holds a double quote.</summary>
    [MessageLogging(EventId = 21010, Level = LogLevel.Error,
        Message = "'{owner}' names the list '{name}' in its {setting} setting, and it is empty, or an item of it is null or holds a double quote, which a quoted list cannot hold")]
    public static partial IGenericMessage ListNotWritable(ILogger logger, string owner, string setting, string name);

    /// <summary>Logs a row of stored options that cannot be read as an option row.</summary>
    [MessageLogging(EventId = 21011, Level = LogLevel.Error,
        Message = "Flow step '{step}' read row {row} of container '{container}', which is not an option row: {reason}")]
    public static partial IGenericMessage RowInvalid(ILogger logger, string step, string container, int row, string reason);

    /// <summary>Logs an option that could not be applied; the failure that caused it comes first in the result.</summary>
    [MessageLogging(EventId = 21012, Level = LogLevel.Error,
        Message = "Flow step '{step}' could not apply option {optionType}.{option} to column '{column}' of container '{container}': {reason}")]
    public static partial IGenericMessage ApplyFailed(ILogger logger, string step, string optionType, string option, string column, string container, string? reason);

    /// <summary>Logs an option whose outcome is not a list, which this step cannot keep.</summary>
    [MessageLogging(EventId = 21013, Level = LogLevel.Error,
        Message = "Flow step '{step}' applied option {optionType}.{option}, whose outcome is not a list of values, and this step keeps a list")]
    public static partial IGenericMessage OutcomeNotAList(ILogger logger, string step, string optionType, string option);

    /// <summary>Logs a settings container that holds more rows for a column than the step may read.</summary>
    [MessageLogging(EventId = 21014, Level = LogLevel.Error,
        Message = "Flow step '{step}' found more than the {maxRows} option row(s) it may read from container '{container}' on data store '{dataStore}'")]
    public static partial IGenericMessage TooManyRows(ILogger logger, string step, string dataStore, string container, int maxRows);

    /// <summary>Logs a read that succeeded and returned no row set.</summary>
    [MessageLogging(EventId = 21015, Level = LogLevel.Error,
        Message = "Flow step '{step}' read container '{container}' on data store '{dataStore}' and got no row set")]
    public static partial IGenericMessage ReadReturnedNothing(ILogger logger, string step, string dataStore, string container);

    /// <summary>Logs the options a step read and the list they left.</summary>
    [MessageLogging(EventId = 21016, Level = LogLevel.Information,
        Message = "Flow step '{step}' read {rows} option row(s) for column '{column}' of container '{container}' and kept {values} value(s) in '{variable}'")]
    public static partial IGenericMessage Read(ILogger logger, string step, int rows, string column, string container, int values, string variable);

    /// <summary>Logs a step whose read of the stored options failed.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "Flow step '{step}' could not read the stored options in container '{container}' on data store '{dataStore}': {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string step, string dataStore, string container, string? reason);
}
