using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authentication.Sessions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Authenticates a request by the server-side session its cookie names (FDW-903).
/// </summary>
/// <remarks>
/// <para>
/// The cookie carries only a random id. The session it names is read from the store, which refuses
/// one that has expired, been revoked, or belongs to another service, and moves its idle end
/// forward. The user-wide revocation that ends a user's bearer tokens ends their sessions too: a
/// session began at its <c>CreatedAt</c>, exactly as a token was issued at its <c>iat</c>.
/// </para>
/// <para>
/// The principal carries the same claims a bearer token from the same flow would have, built from
/// the claim set baked at sign-in by <see cref="SessionPrincipal.Claims"/>, plus <c>sid</c> naming the
/// session. Everything downstream -- the permission checks, the tenant that reaches row-level security,
/// a token minted to carry the caller to another host (FDW-925) -- sees no difference.
/// </para>
/// </remarks>
internal sealed class SessionAuthenticationHandler : IAuthenticationHandler
{
    private readonly IAuthenticationServiceConfigurationProvider _configuration;
    private readonly ISessionAuthenticator _sessions;
    private readonly ILogger _log;

    private AuthenticationScheme? _scheme;
    private HttpContext? _context;

    /// <summary>Initializes a new instance of the <see cref="SessionAuthenticationHandler"/> class.</summary>
    /// <param name="configuration">Reads the declared Session service.</param>
    /// <param name="sessions">Uses the presented session, checks it against the user-wide revocations and builds its principal.</param>
    /// <param name="logger">The logger for validation outcomes.</param>
    public SessionAuthenticationHandler(
        IAuthenticationServiceConfigurationProvider configuration,
        ISessionAuthenticator sessions,
        ILogger<SessionAuthenticationHandler>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _log = logger ?? NullLogger<SessionAuthenticationHandler>.Instance;
    }

    /// <inheritdoc />
    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        _scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<AuthenticateResult> AuthenticateAsync()
    {
        if (_scheme is null || _context is null)
            return AuthenticateResult.NoResult();

        var cancellationToken = _context.RequestAborted;

        var configuration = await ReadConfiguration(cancellationToken).ConfigureAwait(false);
        if (configuration.IsFailure || configuration.Value is not { } service)
            return AuthenticateResult.Fail("Session service is not usable.");

        if (_context.Request.Cookies[service.CookieName] is not { Length: > 0 } sessionId)
            return AuthenticateResult.NoResult();

        // Expiry, revocation and the principal are the authenticator's, shared with every other transport
        // that holds a session; only the cookie and CSRF below are this scheme's own.
        var authenticated = await _sessions.Authenticate(sessionId, ServiceName, _scheme.Name, cancellationToken).ConfigureAwait(false);
        if (authenticated.IsFailure || authenticated.Value is not { } live)
            return AuthenticateResult.Fail("Session is not valid.");

        var session = live.Session;
        var principal = live.Principal;

        // Antiforgery ties its token to the caller it was issued to (the session's principal, when
        // GET /auth/session/csrf ran signed in) and compares that with HttpContext.User at validation.
        // Authentication runs before User is set, so the check sees the session's principal for its
        // duration; otherwise every write is refused as meant for a different claims-based user.
        var previous = _context.User;
        _context.User = principal;
        AuthenticateResult? forged;
        try
        {
            forged = await CsrfRefusal(_context.Request, session, service, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _context.User = previous;
        }

        if (forged is not null)
            return forged;

        SessionLog.SessionAccepted(_log, session.Id, ServiceName, session.UserId);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, _scheme.Name));
    }

    // Only a write is checked. A read changes nothing, and SameSite plus CORS keep another site from
    // reading what it returns; refusing reads would break a plain navigation to the app. Null when the
    // request passes.
    private async Task<AuthenticateResult?> CsrfRefusal(
        HttpRequest request, SessionRecord session, ISessionAuthenticationConfiguration service, CancellationToken cancellationToken)
    {
        if (!IsWrite(request.Method))
            return null;

        var protection = SessionAuthenticationType.CsrfProtectionOf(service, _log);
        if (protection.IsFailure)
            return AuthenticateResult.Fail("Session service is not usable.");

        var verified = await protection.Value!.Verify(request, session, service, _log, cancellationToken).ConfigureAwait(false);
        return verified.IsFailure
            ? AuthenticateResult.Fail("Request failed CSRF protection.")
            : null;
    }

    private static bool IsWrite(string method)
        => !HttpMethods.IsGet(method) && !HttpMethods.IsHead(method)
           && !HttpMethods.IsOptions(method) && !HttpMethods.IsTrace(method);

    /// <inheritdoc />
    /// <remarks>A bare 401, as every other FDW scheme answers.</remarks>
    public Task ChallengeAsync(AuthenticationProperties? properties)
    {
        if (_context is not null)
            _context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ForbidAsync(AuthenticationProperties? properties)
    {
        if (_context is not null)
            _context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    private async Task<IGenericResult<ISessionAuthenticationConfiguration>> ReadConfiguration(CancellationToken cancellationToken)
    {
        var entry = await _configuration.Get(ServiceName, cancellationToken).ConfigureAwait(false);
        if (entry.IsFailure)
            return entry.ToNewResult<ISessionAuthenticationConfiguration>();

        return entry.Value is ISessionAuthenticationConfiguration session
            ? GenericResult<ISessionAuthenticationConfiguration>.Success(session)
            : GenericResult<ISessionAuthenticationConfiguration>.Failure(SessionLog.SessionServiceInvalid(
                _log, ServiceName, "its row is not a Session implementation"));
    }

    // Every Session scheme is named SchemeNameFor(service) by the option that added it, and this is
    // read only after InitializeAsync has set the scheme.
    private string ServiceName => _scheme!.Name[SessionAuthenticationType.SchemePrefix.Length..];
}
