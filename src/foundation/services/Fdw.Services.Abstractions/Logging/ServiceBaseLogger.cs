using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Abstractions.Logging;

/// <summary>
/// Static logger class for ServiceBase operations using MessageLogging infrastructure.
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS3")]
public static partial class ServiceBaseLogger
{
    /// <summary>
    /// Logs when a command type mismatch occurs during execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="expectedType">The expected command type name.</param>
    /// <param name="actualType">The actual command type name received.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Command type mismatch: expected {expectedType}, received {actualType}")]
    public static partial IGenericMessage CommandTypeMismatch(ILogger logger, string expectedType, string actualType);
}
