using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="ExecuteCommand"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class ExecuteCommandLog
{
    /// <summary>Traces an execute command being constructed.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace,
        Message = "[ExecuteCommand] Created with {parameterCount} parameters")]
    public static partial IGenericMessage CommandCreated(ILogger logger, int parameterCount);
}
