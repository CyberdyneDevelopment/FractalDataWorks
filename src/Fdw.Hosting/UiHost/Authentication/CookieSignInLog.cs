using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.UiHost.Authentication;

/// <summary>
/// MessageLogging for the cookie sign-in routes.
/// </summary>
/// <remarks>
/// EventId range: 530-537. Severity is what the event means operationally, not where it sits in the
/// sequence: an attempt is Trace because it happens on every sign-in and is only wanted when
/// following one through; the token exchange is Debug because its timing is the first thing looked
/// at when sign-in feels slow; a rejected password is Warning because it is ordinary but worth
/// counting; a server that answers success with nothing usable is Error because the deployment is
/// misconfigured rather than the caller being wrong.
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class CookieSignInLog
{
    /// <summary>A sign-in was attempted.</summary>
    /// <remarks>
    /// Trace: one per sign-in attempt, and only useful when following a specific attempt through.
    /// The username is included because without it the later messages cannot be tied to this one.
    /// </remarks>
    [MessageLogging(
        EventId = 535,
        Level = LogLevel.Trace,
        Message = "Sign-in attempted for '{username}'")]
    public static partial IGenericMessage SignInAttempted(ILogger logger, string username);

    /// <summary>The token endpoint answered.</summary>
    /// <remarks>
    /// Debug: the elapsed time here is the first thing looked at when sign-in feels slow, and it
    /// separates a slow token endpoint from slow cookie issuance.
    /// </remarks>
    [MessageLogging(
        EventId = 536,
        Level = LogLevel.Debug,
        Message = "Token endpoint answered {statusCode} for '{username}' in {elapsedMs}ms")]
    public static partial IGenericMessage TokenEndpointAnswered(ILogger logger, string username, int statusCode, long elapsedMs);

    /// <summary>A sign-out was attempted.</summary>
    [MessageLogging(
        EventId = 537,
        Level = LogLevel.Trace,
        Message = "Sign-out attempted")]
    public static partial IGenericMessage SignOutAttempted(ILogger logger);

    /// <summary>The token endpoint refused the credentials.</summary>
    [MessageLogging(
        EventId = 530,
        Level = LogLevel.Warning,
        Message = "Sign-in rejected for '{username}': the token endpoint answered {statusCode}")]
    public static partial IGenericMessage SignInRejected(ILogger logger, string username, int statusCode);

    /// <summary>The token endpoint succeeded but returned nothing.</summary>
    /// <remarks>
    /// A success status with a body that deserializes to null — most often a 204 from a server whose
    /// authentication is not actually configured. Worth its own EventId because it looks identical
    /// to a rejected password from the caller's side.
    /// </remarks>
    [MessageLogging(
        EventId = 531,
        Level = LogLevel.Error,
        Message = "Sign-in for '{username}' got {statusCode} with a body that deserialized to nothing")]
    public static partial IGenericMessage SignInEmptyResponse(ILogger logger, string username, int statusCode);

    /// <summary>The token response carried no access token.</summary>
    [MessageLogging(
        EventId = 532,
        Level = LogLevel.Error,
        Message = "Sign-in for '{username}' returned a token response with no access token")]
    public static partial IGenericMessage SignInNoAccessToken(ILogger logger, string username);

    /// <summary>A cookie was issued.</summary>
    [MessageLogging(
        EventId = 533,
        Level = LogLevel.Information,
        Message = "Signed in '{username}'")]
    public static partial IGenericMessage SignInSucceeded(ILogger logger, string username);

    /// <summary>The cookie was cleared.</summary>
    [MessageLogging(
        EventId = 534,
        Level = LogLevel.Information,
        Message = "Signed out")]
    public static partial IGenericMessage SignedOut(ILogger logger);
}
