using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="TruncateCommand"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class TruncateCommandLog
{
    /// <summary>Traces a truncate command being constructed.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "[TruncateCommand] Created")]
    public static partial IGenericMessage CommandCreated(ILogger logger);
}
