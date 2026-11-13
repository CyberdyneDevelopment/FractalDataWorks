using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Options.Logging;

/// <summary>
/// MessageLogging for applying data options.
/// </summary>
/// <remarks>
/// Each failure here shares its EventId with the paired <c>DataOptionResultCodeBase</c> option in
/// <c>Fdw.Data.Options.Results</c>: same number, same meaning. The number is the identity; the DATAOPTION prefix says which
/// package raised it.
/// </remarks>
[MessageLoggingTypeCode("DATAOPTION")]
public static partial class DataOptionLog
{
    /// <summary>Traces entry into applying an option.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "[DataOptions] Applying {optionType}.{option} to column '{column}' of container '{container}' in data store '{dataStore}'")]
    public static partial IGenericMessage Applying(ILogger logger, string optionType, string option, string column, string container, string dataStore);

    /// <summary>Traces an option that was applied.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "[DataOptions] Applied {optionType}.{option} to column '{column}': {count} values are selected")]
    public static partial IGenericMessage Applied(ILogger logger, string optionType, string option, string column, int count);

    /// <summary>Logs an option that needs a value and the row has none.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "[DataOptions] Option {optionType}.{option} needs a value, and the row for column '{column}' of container '{container}' has none")]
    public static partial IGenericMessage ValueRequired(ILogger logger, string optionType, string option, string column, string container);

    /// <summary>Logs an option name that its option type does not have.</summary>
    [MessageLogging(EventId = 30000, Level = LogLevel.Error,
        Message = "[DataOptions] Option type {optionType} has no option named '{option}'")]
    public static partial IGenericMessage OptionNotFound(ILogger logger, string optionType, string option);

    /// <summary>Logs an option type that is not one of the data option types.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error,
        Message = "[DataOptions] '{optionType}' is not a data option type")]
    public static partial IGenericMessage OptionTypeNotFound(ILogger logger, string optionType);
}
