using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="BulkUpsertCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class BulkUpsertCommandLog
{
    /// <summary>Traces a bulk upsert command being constructed.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "[BulkUpsertCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);
}
