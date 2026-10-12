using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="DeleteCommand"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class DeleteCommandLog
{
    /// <summary>Traces a delete command being constructed.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "[DeleteCommand] Created")]
    public static partial IGenericMessage CommandCreated(ILogger logger);
}
