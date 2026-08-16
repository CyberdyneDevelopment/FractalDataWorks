using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="InsertCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class InsertCommandLog
{
    /// <summary>Traces an insert command being constructed.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "[InsertCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);
}
