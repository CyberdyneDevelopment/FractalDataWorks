using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.TokenManagers.Logging;

/// <summary>
/// MessageLogging methods for the generic authentication service (<c>AuthenticationService</c>) and
/// TokenManagers domain plumbing. Every log message is returned in the result AND logged.
/// EventId range: 7437-7456 (see EVENTID-ALLOCATION.md).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("TOKENMANAGERS")]
public static partial class TokenManagerLog
{
    /// <summary>Logs that a token issuance request was null.</summary>
    [MessageLogging(
        EventId = 7437,
        Level = LogLevel.Error,
        Message = "Token issuance request was null")]
    public static partial IGenericMessage RequestNull(ILogger logger);

    /// <summary>Logs the start of authentication for a grant type.</summary>
    [MessageLogging(
        EventId = 7438,
        Level = LogLevel.Trace,
        Message = "Authenticating grant type '{grantType}'")]
    public static partial IGenericMessage AuthenticatingGrant(ILogger logger, string grantType);

    /// <summary>Logs that a credential grant's subject was missing or not a valid identifier.</summary>
    [MessageLogging(
        EventId = 7439,
        Level = LogLevel.Warning,
        Message = "Grant type '{grantType}' requires a valid subject identifier")]
    public static partial IGenericMessage SubjectInvalid(ILogger logger, string grantType);

    /// <summary>Logs that a credential grant's credential value was missing.</summary>
    [MessageLogging(
        EventId = 7440,
        Level = LogLevel.Warning,
        Message = "Grant type '{grantType}' requires a credential")]
    public static partial IGenericMessage CredentialMissing(ILogger logger, string grantType);

    /// <summary>Logs that credential verification denied access.</summary>
    [MessageLogging(
        EventId = 7441,
        Level = LogLevel.Warning,
        Message = "Credential verification denied access for grant type '{grantType}'")]
    public static partial IGenericMessage CredentialDenied(ILogger logger, string grantType);

    /// <summary>Logs that no active token manager is configured.</summary>
    [MessageLogging(
        EventId = 7442,
        Level = LogLevel.Error,
        Message = "No active token manager is configured")]
    public static partial IGenericMessage NoActiveTokenManager(ILogger logger);

    /// <summary>Logs that more than one active token manager was found.</summary>
    [MessageLogging(
        EventId = 7443,
        Level = LogLevel.Error,
        Message = "Expected exactly one active token manager but found {count}")]
    public static partial IGenericMessage MultipleActiveTokenManagers(ILogger logger, int count);

    /// <summary>Logs that token issuance failed.</summary>
    [MessageLogging(
        EventId = 7444,
        Level = LogLevel.Error,
        Message = "Token issuance failed for grant type '{grantType}'")]
    public static partial IGenericMessage IssuanceFailed(ILogger logger, string grantType);

    /// <summary>Logs that token issuance succeeded.</summary>
    [MessageLogging(
        EventId = 7445,
        Level = LogLevel.Information,
        Message = "Token issued successfully for grant type '{grantType}'")]
    public static partial IGenericMessage IssuanceSucceeded(ILogger logger, string grantType);

    /// <summary>Logs that a bearer token was null or empty.</summary>
    [MessageLogging(
        EventId = 7446,
        Level = LogLevel.Error,
        Message = "Bearer token was null or empty")]
    public static partial IGenericMessage TokenMissing(ILogger logger);

    /// <summary>Logs the start of bearer token validation.</summary>
    [MessageLogging(
        EventId = 7447,
        Level = LogLevel.Trace,
        Message = "Validating bearer token")]
    public static partial IGenericMessage ValidatingBearerToken(ILogger logger);

    /// <summary>Logs that bearer token validation failed.</summary>
    [MessageLogging(
        EventId = 7448,
        Level = LogLevel.Warning,
        Message = "Bearer token validation failed")]
    public static partial IGenericMessage ValidationFailed(ILogger logger);

    /// <summary>Logs that bearer token validation succeeded.</summary>
    [MessageLogging(
        EventId = 7449,
        Level = LogLevel.Information,
        Message = "Bearer token validated successfully")]
    public static partial IGenericMessage ValidationSucceeded(ILogger logger);

    /// <summary>Logs that claims extraction failed for a validated token.</summary>
    [MessageLogging(
        EventId = 7450,
        Level = LogLevel.Error,
        Message = "Claims extraction failed for a validated token")]
    public static partial IGenericMessage ClaimsExtractionFailed(ILogger logger);

    /// <summary>Logs the start of a full logout (revoke-all-sessions) operation.</summary>
    [MessageLogging(
        EventId = 7451,
        Level = LogLevel.Trace,
        Message = "Logout started")]
    public static partial IGenericMessage LogoutStarted(ILogger logger);

    /// <summary>Logs that the presented token carried no subject claim, so logout cannot proceed.</summary>
    [MessageLogging(
        EventId = 7452,
        Level = LogLevel.Error,
        Message = "Logout failed: presented token carries no subject claim")]
    public static partial IGenericMessage LogoutSubjectMissing(ILogger logger);

    /// <summary>Logs that revoking the subject's sessions failed.</summary>
    [MessageLogging(
        EventId = 7453,
        Level = LogLevel.Error,
        Message = "Logout failed: could not revoke sessions for subject '{subjectId}'")]
    public static partial IGenericMessage LogoutFailed(ILogger logger, string subjectId);

    /// <summary>Logs that a full logout completed successfully.</summary>
    [MessageLogging(
        EventId = 7454,
        Level = LogLevel.Information,
        Message = "Logout succeeded for subject '{subjectId}'")]
    public static partial IGenericMessage LogoutSucceeded(ILogger logger, string subjectId);

    /// <summary>Logs that a credential grant's subject (username) could not be resolved to a user.</summary>
    [MessageLogging(
        EventId = 7455,
        Level = LogLevel.Warning,
        Message = "Grant type '{grantType}' subject '{subject}' could not be resolved to a user")]
    public static partial IGenericMessage SubjectNotResolved(ILogger logger, string grantType, string subject);

    /// <summary>Logs that the agent_key grant was requested but no agent-key service is registered.</summary>
    [MessageLogging(
        EventId = 7456,
        Level = LogLevel.Error,
        Message = "Grant type 'agent_key' requires an IAgentKeyService, but none is registered")]
    public static partial IGenericMessage AgentKeyServiceNotConfigured(ILogger logger);
}
