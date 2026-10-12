using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="BulkInsertCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class BulkInsertCommandLog
{
    /// <summary>Traces a bulk insert command being constructed.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "[BulkInsertCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);
}
