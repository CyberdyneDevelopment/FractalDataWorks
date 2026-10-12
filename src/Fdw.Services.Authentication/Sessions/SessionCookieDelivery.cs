using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authentication.Sessions.Csrf;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// Sets, clears and serves the session cookie, from the Session service a flow or request names
/// (FDW-903).
/// </summary>
/// <remarks>
/// The cookie is always HttpOnly (script cannot read it), Secure (it never crosses plain HTTP),
/// Path=/ and host-only (no Domain, so no sibling host receives it). Those are not read from
/// configuration: relaxing any one of them hands the id to script or to another host. Its SameSite
/// mode and name are the service's. It expires with the session's absolute end, so a browser does not
/// keep presenting an id the server has already ended.
/// </remarks>
public sealed class SessionCookieDelivery : ISessionCookieDelivery
{
    private readonly IAuthenticationServiceConfigurationProvider _configuration;
    private readonly ISessionStore _sessions;
    private readonly ILogger<SessionCookieDelivery> _logger;

    /// <summary>Initializes a new instance of the <see cref="SessionCookieDelivery"/> class.</summary>
    /// <param name="configuration">Reads the Session service a flow or request names.</param>
    /// <param name="sessions">The session store.</param>
    /// <param name="logger">The logger.</param>
    public SessionCookieDelivery(
        IAuthenticationServiceConfigurationProvider configuration,
        ISessionStore sessions,
        ILogger<SessionCookieDelivery>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _logger = logger ?? NullLogger<SessionCookieDelivery>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Deliver(
        HttpContext context, AuthenticationFlow flow, IssuedToken session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(session);

        if (flow.SessionServiceName is not { } serviceName)
            return GenericResult.Failure(SessionLog.SessionServiceNotDeclared(_logger, flow.Name, "(none)"));

        var service = await Service(serviceName, flow.Name, cancellationToken).ConfigureAwait(false);
        if (service.IsFailure)
            return service;

        var sameSite = SessionAuthenticationType.SameSiteOf(service.Value!, _logger);
        if (sameSite.IsFailure)
            return sameSite;

        context.Response.Cookies.Append(service.Value!.CookieName, session.AccessToken, Cookie(sameSite.Value, session.ExpiresAt));
        SessionLog.SessionCookieSet(_logger, service.Value.CookieName, serviceName, session.ExpiresAt);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Replace(
        HttpContext context, AuthenticationFlow flow, IssuedToken session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SessionOf(context.User) is not { } current)
            return GenericResult.Failure(SessionLog.NotASessionRequest(_logger, context.Request.Path, "replace"));

        // The new session is set only once the old one is ended: a caller who switched tenant must not
        // keep a live session in the tenant they left, however briefly.
        var revoked = await _sessions.Revoke(current.SessionId, cancellationToken).ConfigureAwait(false);
        if (revoked.IsFailure)
            return revoked;

        return await Deliver(context, flow, session, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> End(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SessionOf(context.User) is not { } current)
            return GenericResult.Failure(SessionLog.NotASessionRequest(_logger, context.Request.Path, "end"));

        var service = await Service(current.ServiceName, "(logout)", cancellationToken).ConfigureAwait(false);
        if (service.IsFailure)
            return service;

        var sameSite = SessionAuthenticationType.SameSiteOf(service.Value!, _logger);
        if (sameSite.IsFailure)
            return sameSite;

        var revoked = await _sessions.Revoke(current.SessionId, cancellationToken).ConfigureAwait(false);
        if (revoked.IsFailure)
            return revoked;

        // Cleared only once the server has ended it: clearing first and then failing to revoke would
        // leave a live session the browser can no longer see or end.
        context.Response.Cookies.Delete(service.Value!.CookieName, Cookie(sameSite.Value, expires: null));
        SessionLog.SessionCookieCleared(_logger, service.Value.CookieName, current.ServiceName);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> IssueCsrfToken(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SessionOf(context.User) is not { } current)
            return GenericResult<string>.Failure(SessionLog.NotASessionRequest(_logger, context.Request.Path, "issue a CSRF token for"));

        var service = await Service(current.ServiceName, "(issue csrf token)", cancellationToken).ConfigureAwait(false);
        if (service.IsFailure)
            return service.ToNewResult<string>();

        // Most protections verify against material the session store already holds and issue
        // nothing of their own. Antiforgery is the exception (FDW-945): it mints its own token pair
        // through IAntiforgery, so issuance routes to it only when the configured protection opts in.
        var protection = SessionCsrfProtectionTypes.ByName(service.Value!.CsrfProtection);
        return protection is ICsrfTokenIssuer issuer
            ? issuer.IssueToken(context, current.SessionId, _logger)
            : await _sessions.IssueCsrfToken(current.SessionId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool IsSessionRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SessionOf(context.User) is not null;
    }

    // A principal came from a Session scheme when its identity was built by one: the handler names the
    // identity after its scheme, and every Session scheme's name carries the prefix. The sid it carries
    // is the session's own Id, never the id the browser presented.
    private static (string ServiceName, Guid SessionId)? SessionOf(ClaimsPrincipal principal)
        => principal.Identity is { IsAuthenticated: true, AuthenticationType: { } scheme }
           && scheme.StartsWith(SessionAuthenticationType.SchemePrefix, StringComparison.Ordinal)
           && Guid.TryParse(principal.FindFirst(ClaimDefinitions.sid.Name)?.Value, out var sessionId)
            ? (scheme[SessionAuthenticationType.SchemePrefix.Length..], sessionId)
            : null;

    private async Task<IGenericResult<ISessionAuthenticationConfiguration>> Service(
        string serviceName, string flowName, CancellationToken cancellationToken)
    {
        var entry = await _configuration.Get(serviceName, cancellationToken).ConfigureAwait(false);
        if (entry.IsFailure)
        {
            SessionLog.SessionServiceNotDeclared(_logger, flowName, serviceName);
            return entry.ToNewResult<ISessionAuthenticationConfiguration>();
        }

        return entry.Value is ISessionAuthenticationConfiguration { Enabled: true } service
            ? GenericResult<ISessionAuthenticationConfiguration>.Success(service)
            : GenericResult<ISessionAuthenticationConfiguration>.Failure(
                SessionLog.SessionServiceNotDeclared(_logger, flowName, serviceName));
    }

    private static CookieOptions Cookie(SameSiteMode sameSite, DateTimeOffset? expires)
        => new()
        {
            HttpOnly = true,
            Secure = true,
            Path = "/",
            SameSite = sameSite,
            IsEssential = true,
            Expires = expires,
        };
}
