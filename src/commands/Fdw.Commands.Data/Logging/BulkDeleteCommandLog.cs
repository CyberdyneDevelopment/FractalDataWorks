using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="BulkDeleteCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class BulkDeleteCommandLog
{
    /// <summary>Traces a bulk delete command being constructed.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace,
        Message = "[BulkDeleteCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);
}
