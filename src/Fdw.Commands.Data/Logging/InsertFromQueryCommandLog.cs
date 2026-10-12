using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="InsertFromQueryCommand"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class InsertFromQueryCommandLog
{
    /// <summary>Traces an insert-from-query command being constructed.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace,
        Message = "[InsertFromQueryCommand] Created with {columnCount} target columns")]
    public static partial IGenericMessage CommandCreated(ILogger logger, int columnCount);
}
