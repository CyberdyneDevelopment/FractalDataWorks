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
    [MessageLogging(EventId = 11411, Level = LogLevel.Error, Message = "The script's result has the column '{column}' more than once; give one of them another name with AS")]
    public static partial IGenericMessage ColumnRepeated(ILogger logger, string column);

    // Why Error: the script selected more than it was allowed to read, and the read stops rather than keep part of it.
    /// <summary>A script selected more rows than it may read.</summary>
    [MessageLogging(EventId = 11412, Level = LogLevel.Error, Message = "The script selects more than the {take} row(s) it may read")]
    public static partial IGenericMessage TooManyRows(ILogger logger, int take);
}
