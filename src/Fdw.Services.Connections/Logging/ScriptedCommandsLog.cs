using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for routing a scripted command to the connection's reading of it (FDW-1441), shared by the SQL connections.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ScriptedCommandsLog
{
    // Why Warning: the command reached the connection unsound; nothing ran and the caller is told what is wrong.
    /// <summary>A script is refused before it runs.</summary>
    [MessageLogging(EventId = 11413, Level = LogLevel.Warning, Message = "The {commandType} script is refused: {problem}")]
    public static partial IGenericMessage Refused(ILogger logger, string commandType, string problem);

    // Why Error: a command added to the family without its reading in this connection.
    /// <summary>The connection has no reading for this scripted command's answer.</summary>
    [MessageLogging(EventId = 11414, Level = LogLevel.Error, Message = "The connection has no reading for the answer of scripted command '{commandType}'")]
    public static partial IGenericMessage NotRead(ILogger logger, string commandType);
}
