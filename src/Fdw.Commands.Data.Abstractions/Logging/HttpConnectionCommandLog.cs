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

    /// <summary>
    /// Logs the defect condition immediately before <see cref="HttpConnectionCommand"/> throws
    /// <see cref="System.ArgumentNullException"/> for a null HTTP method. See the logging-pass
    /// report — the throw itself is left in place.
    /// </summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "[HttpConnectionCommand] HTTP method is required and was null")]
    public static partial IGenericMessage HttpMethodMissing(ILogger logger);

    /// <summary>
    /// Logs the defect condition immediately before <see cref="HttpConnectionCommand"/> throws
    /// <see cref="System.ArgumentNullException"/> for a null relative path. See the logging-pass
    /// report — the throw itself is left in place.
    /// </summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error,
        Message = "[HttpConnectionCommand] Relative path is required and was null")]
    public static partial IGenericMessage RelativePathMissing(ILogger logger);
}
