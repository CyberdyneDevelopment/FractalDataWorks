using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.UiHost.Endpoints.Logging;

/// <summary>
/// MessageLogging for the declared cookie sign-in and sign-out endpoints.
/// </summary>
/// <remarks>
/// EventId range: 11040-11045.
///
/// Why these exist: the exchange itself already narrates what it did — <c>CookieSignInLog</c>
/// covers the token call, the rejection, and the cookie. What it cannot say is whether the request
/// ever reached it. These bases are wrappers around a route that used to be mapped directly, so the
/// failure they exist to make visible is the wrapper never being wired: the endpoint is not
/// declared, the route 404s, and every message in the exchange log is simply absent. Absence is not
/// evidence, so the wrapper reports its own two moments.
///
/// Severity follows what each moment means rather than its position: claiming a route is Trace
/// because it happens once at startup and only matters when reconciling a 404 against what was
/// registered; entry is Trace because it is one line per request; completion is Debug because its
/// elapsed time is what separates a slow token endpoint from a slow cookie write, and it is the one
/// line that proves the handler returned rather than hung.
/// </remarks>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("UIHOSTEP")]
public static partial class CookieSignInEndpointLog
{
    /// <summary>Logs the route the login endpoint claimed as it configured itself.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="route">The route claimed.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11040,
        Level = LogLevel.Trace,
        Message = "Cookie login endpoint configured: POST {route}")]
    public static partial IGenericMessage LoginEndpointConfigured(ILogger logger, string route);

    /// <summary>Logs entry to the login handler.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Trace,
        Message = "Cookie login endpoint handling a request")]
    public static partial IGenericMessage LoginHandling(ILogger logger);

    /// <summary>Logs that the login handler returned.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="elapsedMs">How long the exchange took.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11042,
        Level = LogLevel.Debug,
        Message = "Cookie login endpoint completed in {elapsedMs}ms")]
    public static partial IGenericMessage LoginHandled(ILogger logger, long elapsedMs);

    /// <summary>Logs the route the logout endpoint claimed as it configured itself.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="route">The route claimed.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11043,
        Level = LogLevel.Trace,
        Message = "Cookie logout endpoint configured: GET {route}")]
    public static partial IGenericMessage LogoutEndpointConfigured(ILogger logger, string route);

    /// <summary>Logs entry to the logout handler.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11044,
        Level = LogLevel.Trace,
        Message = "Cookie logout endpoint handling a request")]
    public static partial IGenericMessage LogoutHandling(ILogger logger);

    /// <summary>Logs that the logout handler returned.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="elapsedMs">How long the sign-out took.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11045,
        Level = LogLevel.Debug,
        Message = "Cookie logout endpoint completed in {elapsedMs}ms")]
    public static partial IGenericMessage LogoutHandled(ILogger logger, long elapsedMs);
}
