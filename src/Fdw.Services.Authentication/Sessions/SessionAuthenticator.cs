using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// The one place a session id becomes a principal: used through the store, checked against the
/// user-wide revocations, and built from the claims baked at sign-in.
/// </summary>
/// <remarks>
/// The Session scheme calls it for a cookie and a SQL Server connection calls it for each batch, so a
/// session revoked, expired or idle past its timeout ends for both the same way.
/// </remarks>
internal sealed class SessionAuthenticator : ISessionAuthenticator
{
    private readonly ISessionStore _sessions;
    private readonly ITokenRevocationStore _revocations;
    private readonly ILogger _log;

    /// <summary>Initializes a new instance of the <see cref="SessionAuthenticator"/> class.</summary>
    /// <param name="sessions">Resolves the presented session.</param>
    /// <param name="revocations">This host's deny-list; its user-wide revocations end sessions too.</param>
    /// <param name="logger">The logger for refusals.</param>
    public SessionAuthenticator(
        ISessionStore sessions,
        ITokenRevocationStore revocations,
        ILogger<SessionAuthenticator>? logger = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _revocations = revocations ?? throw new ArgumentNullException(nameof(revocations));
        _log = logger ?? NullLogger<SessionAuthenticator>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AuthenticatedSession>> Authenticate(
        string sessionId, string serviceName, string authenticationType, CancellationToken cancellationToken = default)
    {
        var used = await _sessions.Use(sessionId, serviceName, cancellationToken).ConfigureAwait(false);
        if (used.IsFailure)
            return used.ToNewResult<AuthenticatedSession>();

        if (used.Value is not { } session)
            return GenericResult<AuthenticatedSession>.Failure(SessionLog.SessionReadReturnedNoValue(_log, serviceName));

        // The user-wide revocation that ends a user's bearer tokens ends their sessions too: a session
        // began at its CreatedAt exactly as a token was issued at its iat.
        var revoked = await _revocations
            .IsRevoked(session.Id, session.UserId, session.CreatedAt, cancellationToken)
            .ConfigureAwait(false);
        if (revoked.IsFailure)
            return revoked.ToNewResult<AuthenticatedSession>();

        if (revoked.Value)
            return GenericResult<AuthenticatedSession>.Failure(
                SessionLog.SessionUserRevoked(_log, session.Id, serviceName, session.UserId));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            SessionPrincipal.Claims(session), authenticationType, ClaimDefinitions.sub.Name, ClaimDefinitions.roles.Name));

        return GenericResult<AuthenticatedSession>.Success(new AuthenticatedSession(session, principal));
    }
}
