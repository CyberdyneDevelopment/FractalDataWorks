using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="BulkUpdateCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class BulkUpdateCommandLog
{
    /// <summary>Traces a bulk update command being constructed.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace,
        Message = "[BulkUpdateCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);
}
