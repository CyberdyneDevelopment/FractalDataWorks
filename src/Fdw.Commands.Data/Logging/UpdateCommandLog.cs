using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="UpdateCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class UpdateCommandLog
{
    /// <summary>Traces an update command being constructed.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "[UpdateCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);
}
