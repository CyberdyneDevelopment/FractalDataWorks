using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for building a nearest search (<see cref="NearestCommand{T}"/>).
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class NearestCommandLog
{
    // Why Warning: the caller asked for a search it did not finish describing; nothing ran, and it is told what is missing.
    /// <summary>A value a nearest search needs was not given.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Warning,
        Message = "Required value '{name}' was not provided for the nearest search.")]
    public static partial IGenericMessage RequiredValueMissing(ILogger logger, string name);

    // Why Warning: as above, a value given that no search can use.
    /// <summary>A value given for a nearest search cannot be used.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "Input '{name}' is invalid for the nearest search: {reason}.")]
    public static partial IGenericMessage InvalidInput(ILogger logger, string name, string reason);
}
