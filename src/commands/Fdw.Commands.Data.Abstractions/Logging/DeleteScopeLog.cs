using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging methods for the delete scope rule (<see cref="DeleteScope"/>, FDW-3660).
/// </summary>
[MessageLoggingTypeCode("DELETESCOPE")]
public static partial class DeleteScopeLog
{
    /// <summary>
    /// Logs a delete a translator refused because it does not state the rows it removes.
    /// </summary>
    [MessageLogging(EventId = 20100, Level = LogLevel.Warning,
        Message = "A Delete on container '{containerName}' is refused: {problem}")]
    public static partial IGenericMessage Refused(ILogger logger, string containerName, string problem);
}
