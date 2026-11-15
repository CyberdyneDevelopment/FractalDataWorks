using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="FindCommand{T}"/> construction and for building a find (<c>FindCommandBuilder</c>).
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class FindCommandLog
{
    /// <summary>Traces a find (cross-field search) command being constructed.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "[FindCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);

    // Why Warning: the caller asked for a find it did not finish describing; nothing ran, and it is told what is missing.
    // Why one message: what is missing differs only in its name (the data store, the path, the container, the search term); the meaning
    // is the same, "a required value was not provided" (RequiredValueMissing, 20000), as in NearestCommandLog.
    /// <summary>A value a find needs was not given.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Warning,
        Message = "Required value '{name}' was not provided for the find.")]
    public static partial IGenericMessage RequiredValueMissing(ILogger logger, string name);
}
