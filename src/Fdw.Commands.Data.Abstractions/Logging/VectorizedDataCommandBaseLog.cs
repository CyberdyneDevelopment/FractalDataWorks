using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="VectorizedDataCommandBase"/>.
/// </summary>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class VectorizedDataCommandBaseLog
{
    // Why Trace: one line per command made, the same volume DataCommandBase logs at.
    /// <summary>A vectorized command was made.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "[VectorizedDataCommandBase] Created vectorized command {commandId} of type '{commandType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, Guid commandId, string commandType);
}
