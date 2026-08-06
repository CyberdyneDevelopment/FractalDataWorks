using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.SecretManagers.Abstractions.Logging;

/// <summary>
/// Static logger class for SecretManager operations using MessageLogging infrastructure.
/// </summary>
[MessageLoggingTypeCode("SECRETMANAGER")]
public static partial class SecretManagerLogger
{
    /// <summary>
    /// Logs when validation fails for a secret manager operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Secret manager validation failed: {errorMessage}")]
    public static partial IGenericMessage ValidationFailed(ILogger logger, string errorMessage);
}
