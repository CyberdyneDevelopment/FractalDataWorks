using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="HttpConnectionCommand"/> construction.
/// </summary>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class HttpConnectionCommandLog
{
    /// <summary>Traces an HTTP connection command being constructed.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "[HttpConnectionCommand] Created {method} request for '{relativePath}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string method, string relativePath);
}
