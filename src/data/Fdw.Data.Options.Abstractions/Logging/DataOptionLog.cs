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

    /// <summary>Logs a value count a kind does not take.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "[DataOptions] Kind {kind} on field '{field}' takes {minimum} to {maximum} values and was given {count}")]
    public static partial IGenericMessage ValueCountOutOfRange(ILogger logger, string kind, string field, int minimum, string maximum, int count);

    /// <summary>Logs a path that stops at a layer that has layers below it.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning,
        Message = "[DataOptions] Data option path '{path}' ends at '{name}', which has layers below it ({known}), not at a kind")]
    public static partial IGenericMessage PathEndsBeforeKind(ILogger logger, string path, string name, string known);

    /// <summary>Logs a path that goes on below a kind.</summary>
    [MessageLogging(EventId = 20003, Level = LogLevel.Warning,
        Message = "[DataOptions] Data option path '{path}' names '{extra}' below the kind '{kind}'")]
    public static partial IGenericMessage PathContinuesPastKind(ILogger logger, string path, string extra, string kind);

    /// <summary>Logs a path level that names no layer.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning,
        Message = "[DataOptions] Data option path '{path}': level {level} '{name}' was not found (known: {known})")]
    public static partial IGenericMessage PathLevelNotFound(ILogger logger, string path, int level, string name, string known);

    /// <summary>Logs a method the kind does not have.</summary>
    [MessageLogging(EventId = 30004, Level = LogLevel.Warning,
        Message = "[DataOptions] Data option path '{path}' names method '{method}'; the kind's method is {kindMethod}")]
    public static partial IGenericMessage MethodNotFound(ILogger logger, string path, string method, string kindMethod);

    /// <summary>Traces a kind that was applied.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "[DataOptions] Applied kind {kind} to field '{field}' with {count} values")]
    public static partial IGenericMessage KindApplied(ILogger logger, string kind, string field, int count);
}
