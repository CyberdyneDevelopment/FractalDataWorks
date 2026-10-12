using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="UpsertCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class UpsertCommandLog
{
    /// <summary>Traces an upsert command being constructed.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "[UpsertCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);
}
