using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Structured logging for WebMCP PAT authentication middleware.
/// EventId range: 7200-7205
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("HOSTING")]
public static partial class WebMcpApiKeyMiddlewareLog
{
    /// <summary>Logs when a PAT is validated successfully.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace, Message = "PAT authentication succeeded for user {userId}")]
    public static partial IGenericMessage PatAuthenticationSucceeded(ILogger logger, string userId);

    /// <summary>Logs when a PAT fails validation (invalid, expired, or revoked).</summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning, Message = "PAT authentication failed: token is invalid or expired")]
    public static partial IGenericMessage PatAuthenticationFailed(ILogger logger);

    /// <summary>Logs when PAT validation returns a service-level error.</summary>
    [MessageLogging(EventId = 51001, Level = LogLevel.Error, Message = "PAT validation service error: {error}")]
    public static partial IGenericMessage PatValidationError(ILogger logger, string error);

    /// <summary>Logs when an Authorization header matches the PAT prefix pattern.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace, Message = "PAT Authorization header detected, validating token")]
    public static partial IGenericMessage PatHeaderDetected(ILogger logger);

    /// <summary>Logs when the IPersonalAccessTokenService is not registered in DI.</summary>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error, Message = "IPersonalAccessTokenService is not registered. Call AddMsSqlPersonalAccessTokenService() during startup.")]
    public static partial IGenericMessage PatServiceNotRegistered(ILogger logger);
}
