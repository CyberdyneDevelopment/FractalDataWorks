using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration.Logging;

/// <summary>
/// MessageLogging for the reader of configuration container rows and the search over them (FDW-2444).
/// EventId ranges: 11060-11064 progress, 21060-21063 a refused window or a search that cannot be answered, 61060-61061 a read with no result,
/// 91061-91062 a row the store holds wrongly.
/// </summary>
[MessageLoggingTypeCode("SERVICES")]
public static partial class ConfigurationRowReaderLog
{
    /// <summary>Logs a page of rows read from a container.</summary>
    [MessageLogging(EventId = 11060, Level = LogLevel.Trace,
        Message = "Read {rowCount} row(s) of container {container}, skip {skip}, take {take}")]
    public static partial IGenericMessage PageRead(ILogger logger, string container, int skip, int take, int rowCount);

    /// <summary>Logs a count of the rows a condition selects.</summary>
    [MessageLogging(EventId = 11061, Level = LogLevel.Trace, Message = "Counted {total} row(s) of container {container}")]
    public static partial IGenericMessage Counted(ILogger logger, string container, long total);

    /// <summary>Logs a ranked search over a container.</summary>
    [MessageLogging(EventId = 11062, Level = LogLevel.Debug,
        Message = "Searched container {container} for '{text}': window {window}, {candidateCount} candidate(s) read, {matches} match(es) in all")]
    public static partial IGenericMessage Searched(ILogger logger, string container, string text, int window, int candidateCount, int matches);

    /// <summary>Logs the names read for the fields of a page.</summary>
    [MessageLogging(EventId = 11063, Level = LogLevel.Trace, Message = "Read the names {hopContainer} holds for {keyCount} key(s)")]
    public static partial IGenericMessage HopRead(ILogger logger, string hopContainer, int keyCount);

    /// <summary>Logs a search window that is empty or larger than a search may carry.</summary>
    [MessageLogging(EventId = 21060, Level = LogLevel.Warning,
        Message = "A window of {window} is asked of {container}; a window is from 1 to {maximum}",
        ClientMessage = "A window of {window} is asked; a window is from 1 to {maximum}, so page less deep or narrow the search")]
    public static partial IGenericMessage WindowRefused(ILogger logger, string container, int window, int maximum);

    /// <summary>Logs more keys asked of one lookup than a lookup may carry.</summary>
    [MessageLogging(EventId = 21061, Level = LogLevel.Warning,
        Message = "{keyCount} keys are asked of {container} in one lookup; a lookup carries at most {maximum}")]
    public static partial IGenericMessage LookupRefused(ILogger logger, string container, int keyCount, int maximum);

    /// <summary>Logs a count that returned no number.</summary>
    [MessageLogging(EventId = 61060, Level = LogLevel.Error, Message = "The count over {container} returned no number")]
    public static partial IGenericMessage CountReturnedNoResult(ILogger logger, string container);

    /// <summary>Logs a row that a hit points at and the caller cannot read, so the hit is left out of the page.</summary>
    [MessageLogging(EventId = 11064, Level = LogLevel.Debug,
        Message = "Row {key} of container {container} is not readable by the caller or not held, so the things under it are left out")]
    public static partial IGenericMessage RowNotReadable(ILogger logger, string container, long key);

    /// <summary>Logs a read that succeeded and returned no rows collection.</summary>
    [MessageLogging(EventId = 61061, Level = LogLevel.Error, Message = "Reading container {container} (skip {skip}, take {take}) succeeded and returned no result")]
    public static partial IGenericMessage PageReturnedNoResult(ILogger logger, string container, int skip, int take);

    /// <summary>Logs a row that lacks a column the query asked for.</summary>
    [MessageLogging(EventId = 91062, Level = LogLevel.Error, Message = "A row of container {container} lacks a column the search reads: {reason}")]
    public static partial IGenericMessage ColumnMissing(ILogger logger, string container, string reason);
}
