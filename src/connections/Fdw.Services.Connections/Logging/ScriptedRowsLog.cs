using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for reading the rows a script selects (FDW-1441), shared by the SQL connections.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ScriptedRowsLog
{
    // Why Debug: one line per script, with what it found; the gateway logs the command's completion above it.
    /// <summary>A script's rows were read.</summary>
    [MessageLogging(EventId = 11410, Level = LogLevel.Debug, Message = "Script read {rows} row(s)")]
    public static partial IGenericMessage Read(ILogger logger, int rows);

    // Why Error: a column named twice cannot be a row keyed by name, and keeping one would lose a value silently.
    /// <summary>A script's result names a column twice.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error, Message = "The script's result has the column '{column}' more than once; give one of them another name with AS", ClientMessage = "The script's result has the column '{column}' more than once; give one of them another name with AS")]
    public static partial IGenericMessage ColumnRepeated(ILogger logger, string column);

    // Why Error: the script selected more than it was allowed to read, and the read stops rather than keep part of it.
    /// <summary>A script selected more rows than it may read.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Error, Message = "The script selects more than the {take} row(s) it may read", ClientMessage = "The script selects more than the {take} row(s) it may read")]
    public static partial IGenericMessage TooManyRows(ILogger logger, int take);

    // Why Error: a streamed script reads from an open network reader, which is read asynchronously; a synchronous read would block a thread on the network.
    /// <summary>A streamed script's rows were asked for synchronously.</summary>
    [MessageLogging(EventId = 20005, Level = LogLevel.Error, Message = "A streamed script's rows are read with Read(cancellationToken), never synchronously", ClientMessage = "A streamed script's rows are read with Read(cancellationToken), never synchronously")]
    public static partial IGenericMessage ReadOnlyAsynchronously(ILogger logger);

    // Why Error: the store failed after rows were already handed on, so the read is a failure even though part of it arrived (FDW-4903).
    /// <summary>A streamed script's read failed part way through.</summary>
    [MessageLogging(EventId = 20006, Level = LogLevel.Error, Message = "The script's read failed after {rows} row(s) had been read", ClientMessage = "The script's read failed after {rows} row(s) had been read")]
    public static partial IGenericMessage ReadFailedPartWay(ILogger logger, int rows);

    // Why Error: a script's reader is forward-only, so a second read would answer no rows and look like an empty result.
    /// <summary>A streamed script's rows were asked for a second time.</summary>
    [MessageLogging(EventId = 20007, Level = LogLevel.Error, Message = "A streamed script's rows can be read once; this one has already been read", ClientMessage = "A streamed script's rows can be read once; this one has already been read")]
    public static partial IGenericMessage ReadAgain(ILogger logger);
}
