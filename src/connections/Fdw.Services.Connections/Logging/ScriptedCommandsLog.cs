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
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning, Message = "The {commandType} script is refused: {problem}", ClientMessage = "The {commandType} script is refused")]
    public static partial IGenericMessage Refused(ILogger logger, string commandType, string problem);

    // Why Error: a command added to the family without its reading in this connection.
    /// <summary>The connection has no reading for this scripted command's answer.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The connection has no reading for the answer of scripted command '{commandType}'", ClientMessage = "The connection has no reading for the answer of scripted command '{commandType}'")]
    public static partial IGenericMessage NotRead(ILogger logger, string commandType);

    // Why Configuration (6xxxx) and its own message: a script source sends Stream (FDW-4903), and a script connection a consumer wrote before
    // that routes only Read and Run is a host set up without the stream route (FDW-4843's rule: a configuration fault, not
    // Internal). It must say what to change, not the generic "no reading". The gateway names the connection
    // that raised it (its Id, in the failure's details) and the script source names the data store.
    /// <summary>A script connection that does not stream was sent a Stream.</summary>
    [MessageLogging(EventId = 60010, Level = LogLevel.Error, Message = "This script connection does not route Stream; a script source needs ScriptedCommands.Route with the stream delegate", ClientMessage = "This script connection does not route Stream; a script source needs ScriptedCommands.Route with the stream delegate")]
    public static partial IGenericMessage StreamNotRouted(ILogger logger);
}
