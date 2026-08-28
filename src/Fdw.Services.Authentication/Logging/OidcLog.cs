using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for the OIDC redirect step.
/// </summary>
/// <remarks>
/// EventId range: 91210–91219. No code, state, verifier, nonce or token appears at any level — each
/// is either a credential or a means of completing someone else's exchange.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class OidcLog
{
    /// <summary>The caller is being sent to the provider.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91210, Level = LogLevel.Trace,
        Message = "Challenging to '{issuer}'")]
    internal static partial IGenericMessage Challenging(ILogger<OidcRedirectStep> logger, string issuer);

    /// <summary>The caller came back and their token verified.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91211, Level = LogLevel.Trace,
        Message = "Completed the exchange with '{issuer}'")]
    internal static partial IGenericMessage Completed(ILogger<OidcRedirectStep> logger, string issuer);

    /// <summary>The callback carried a code but no state.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91212, Level = LogLevel.Warning,
        Message = "A callback from '{issuer}' carried no state and cannot be matched to a request")]
    internal static partial IGenericMessage StateMissing(ILogger<OidcRedirectStep> logger, string issuer);

    /// <summary>The stored request was made to a different provider.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="expected">The provider this step is configured for.</param>
    /// <param name="actual">The provider the request was made to.</param>
    [MessageLogging(EventId = 91213, Level = LogLevel.Warning,
        Message = "A request made to '{actual}' was returned to the step for '{expected}'")]
    internal static partial IGenericMessage IssuerMismatch(
        ILogger<OidcRedirectStep> logger, string expected, string actual);

    /// <summary>The provider refused the code exchange.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="statusCode">What it answered.</param>
    [MessageLogging(EventId = 91214, Level = LogLevel.Warning,
        Message = "'{issuer}' refused the code exchange with status {statusCode}")]
    internal static partial IGenericMessage ExchangeRefused(
        ILogger<OidcRedirectStep> logger, string issuer, string statusCode);

    /// <summary>The token endpoint could not be reached.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="failure">The kind of failure.</param>
    [MessageLogging(EventId = 91215, Level = LogLevel.Error,
        Message = "Could not reach the token endpoint at '{issuer}': {failure}")]
    internal static partial IGenericMessage ExchangeFailed(
        ILogger<OidcRedirectStep> logger, string issuer, string failure);

    /// <summary>The exchange succeeded but returned no identity token.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91216, Level = LogLevel.Error,
        Message = "'{issuer}' returned no id_token — check that the openid scope is requested")]
    internal static partial IGenericMessage NoIdToken(ILogger<OidcRedirectStep> logger, string issuer);

    /// <summary>The returned token failed validation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="failure">The kind of check that failed.</param>
    [MessageLogging(EventId = 91217, Level = LogLevel.Warning,
        Message = "A token from '{issuer}' was rejected: {failure}")]
    internal static partial IGenericMessage TokenRejected(
        ILogger<OidcRedirectStep> logger, string issuer, string failure);

    /// <summary>The token did not echo the nonce this platform sent.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    [MessageLogging(EventId = 91218, Level = LogLevel.Warning,
        Message = "A token from '{issuer}' did not echo the nonce for this request")]
    internal static partial IGenericMessage NonceMismatch(ILogger<OidcRedirectStep> logger, string issuer);

    /// <summary>The token carried no subject under the configured claim.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider.</param>
    /// <param name="claim">The claim that was expected.</param>
    [MessageLogging(EventId = 91219, Level = LogLevel.Error,
        Message = "A token from '{issuer}' carried no '{claim}' claim to bind on")]
    internal static partial IGenericMessage SubjectClaimMissing(
        ILogger<OidcRedirectStep> logger, string issuer, string claim);
}
