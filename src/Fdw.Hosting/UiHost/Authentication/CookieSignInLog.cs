using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.UiHost.Authentication;

/// <summary>
/// MessageLogging for the cookie sign-in routes.
/// </summary>
/// <remarks>EventId range: 530-534.</remarks>
[ExcludeFromCodeCoverage]
public static partial class CookieSignInLog
{
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
