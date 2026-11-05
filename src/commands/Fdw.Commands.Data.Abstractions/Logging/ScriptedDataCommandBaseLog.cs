using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="ScriptedDataCommandBase"/>.
/// </summary>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class ScriptedDataCommandBaseLog
{
    // Why Trace: one line per command made, the same volume DataCommandBase logs at.
    /// <summary>A scripted command was made.</summary>
    [MessageLogging(EventId = 11290, Level = LogLevel.Trace,
        Message = "[ScriptedDataCommandBase] Created scripted command {commandId} of type '{commandType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, Guid commandId, string commandType);

    /// <summary>The first thing wrong with a scripted command itself, found before any store is reached (FDW-3580).</summary>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error,
        Message = "The scripted command is not sound: {reason}")]
    public static partial IGenericMessage CommandUnsound(ILogger logger, string reason);
}
