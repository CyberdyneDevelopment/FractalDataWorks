using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="FindCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class FindCommandLog
{
    /// <summary>Traces a find (cross-field search) command being constructed.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "[FindCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);
}
