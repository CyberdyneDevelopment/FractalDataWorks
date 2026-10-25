using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>Starts the session a person acts with a role in (FDW-2282).</summary>
/// <remarks>
/// The narrowing is <see cref="ActingGrant"/>, which asks "does the caller hold what the role carries" through the one coverage rule every
/// request is judged by. It is not the grant rule, which asks whether the caller may give the role away and also demands the delegate right.
/// What the session holds is read from its own <c>perm</c> claims, before any role is expanded, so the expansion's own system context
/// cannot widen the answer.
/// </remarks>
public sealed class SessionActing : ISessionActing
{
    private readonly ISessionStore _sessions;
    private readonly ISessionCookieDelivery _cookies;
    private readonly IRoleConfigurationProvider _roles;
    private readonly IRoleGrants _grants;
    private readonly ISystemRoleConfiguration _systemRoles;
    private readonly ILogger<SessionActing> _logger;

    /// <summary>Initializes a new instance of the <see cref="SessionActing"/> class.</summary>
    /// <param name="sessions">The session store.</param>
    /// <param name="cookies">Points the response's cookie at the new session.</param>
    /// <param name="roles">Reads the role catalogue.</param>
    /// <param name="grants">Expands a role to the permissions it carries.</param>
    /// <param name="systemRoles">Tells whether the caller administers the platform.</param>
    /// <param name="logger">The logger.</param>
    public SessionActing(
        ISessionStore sessions,
        ISessionCookieDelivery cookies,
        IRoleConfigurationProvider roles,
        IRoleGrants grants,
        ISystemRoleConfiguration systemRoles,
        ILogger<SessionActing>? logger = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _cookies = cookies ?? throw new ArgumentNullException(nameof(cookies));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _grants = grants ?? throw new ArgumentNullException(nameof(grants));
        _systemRoles = systemRoles ?? throw new ArgumentNullException(nameof(systemRoles));
        _logger = logger ?? NullLogger<SessionActing>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Act(HttpContext context, string roleName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        if (SessionPrincipal.Identity(context.User) is not { } identity)
            return GenericResult.Failure(SessionLog.NotASessionRequest(_logger, context.Request.Path, "act with a role in"));

        var current = SessionPrincipal.Assertion(identity, DateTimeOffset.MaxValue, _logger);
        if (current.IsFailure)
            return current;

        var role = await _roles.Get(roleName, cancellationToken).ConfigureAwait(false);
        if (role.IsFailure || role.Value is null)
            return GenericResult.Failure(SessionLog.ActingRoleUnknown(_logger, roleName));

        // Why the caller is told the role does not exist: a role of another company would otherwise be told apart from an unknown one,
        // and a name could be probed for. The log line keeps both companies for the operator.
        if (role.Value.IsTenantScoped && role.Value.TenantId != current.Value!.TenantId)
        {
            SessionLog.ActingRoleOtherTenant(
                _logger, roleName, role.Value.TenantId?.ToString() ?? "(none)", current.Value.TenantId.ToString());
            return GenericResult.Failure(SessionLog.ActingRoleUnknown(_logger, roleName));
        }

        var carried = await _grants.Get([role.Value.Name], cancellationToken).ConfigureAwait(false);
        if (carried.IsFailure)
            return carried;

        IReadOnlyList<string> refused = _systemRoles.IsPlatformAdmin(context.User)
            ? []
            : ActingGrant.NotCovered(identity.FindAll(ClaimDefinitions.perm.Name).Select(claim => claim.Value), carried.Value!);
        if (refused.Count > 0)
            return GenericResult.Failure(SessionLog.ActingRightsNotHeld(_logger, role.Value.Name, string.Join(", ", refused)));

        var narrowed = current.Value! with
        {
            Claims = new Dictionary<string, IReadOnlyList<string>>(current.Value.Claims, StringComparer.Ordinal)
            {
                [ClaimDefinitions.roles.Name] = [role.Value.Name],
                [ClaimDefinitions.perm.Name] = [.. carried.Value!],
                [ClaimDefinitions.actor.Name] = [current.Value.PrincipalId.ToString()],
                [ClaimDefinitions.actingRole.Name] = [role.Value.Name],
            },
        };

        var parentId = Guid.Parse(identity.FindFirst(ClaimDefinitions.sid.Name)!.Value);
        var started = await _sessions.Act(parentId, narrowed, cancellationToken).ConfigureAwait(false);
        if (started.IsFailure)
            return started;

        // The cookie moves only once the session above exists and the one below sleeps: a failure to set it leaves the person
        // holding a cookie for a sleeping session, which the next request answers by sending them to sign in again, never by widening.
        var delivered = await _cookies.Set(context, started.Value!, cancellationToken).ConfigureAwait(false);
        if (delivered.IsFailure)
            return delivered;

        SessionLog.ActingStarted(_logger, current.Value.PrincipalId, role.Value.Name, parentId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Return(HttpContext context, int step, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SessionPrincipal.Identity(context.User) is not { } identity)
            return GenericResult.Failure(SessionLog.NotASessionRequest(_logger, context.Request.Path, "return from acting in"));

        var woken = await _sessions.Return(Guid.Parse(identity.FindFirst(ClaimDefinitions.sid.Name)!.Value), step, cancellationToken).ConfigureAwait(false);
        if (woken.IsFailure)
            return woken;

        // The cookie moves once the sessions above are ended and the step is awake. The new id replaces the old, whose hash no longer matches.
        return await _cookies.Set(context, woken.Value!, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<ActingStep>>> Steps(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SessionPrincipal.Identity(context.User) is { } identity
            ? await _sessions.Chain(Guid.Parse(identity.FindFirst(ClaimDefinitions.sid.Name)!.Value), cancellationToken).ConfigureAwait(false)
            : GenericResult<IReadOnlyList<ActingStep>>.Failure(SessionLog.NotASessionRequest(_logger, context.Request.Path, "list the steps of"));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<string>>> Roles(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SessionPrincipal.Identity(context.User) is not { } identity)
            return GenericResult<IReadOnlyList<string>>.Failure(SessionLog.NotASessionRequest(_logger, context.Request.Path, "list the roles to act with in"));

        var current = SessionPrincipal.Assertion(identity, DateTimeOffset.MaxValue, _logger);
        if (current.IsFailure)
            return current.ToNewResult<IReadOnlyList<string>>();

        var all = await _roles.Get(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure)
            return all.ToNewResult<IReadOnlyList<string>>();

        var held = identity.FindAll(ClaimDefinitions.perm.Name).Select(claim => claim.Value).ToList();
        var administers = _systemRoles.IsPlatformAdmin(context.User);
        var offered = new List<string>();
        foreach (var role in all.Value!.Where(role => !role.IsTenantScoped || role.TenantId == current.Value!.TenantId))
        {
            var carried = await _grants.Get([role.Name], cancellationToken).ConfigureAwait(false);
            if (carried.IsFailure)
                return carried.ToNewResult<IReadOnlyList<string>>();

            if (administers || ActingGrant.NotCovered(held, carried.Value!).Count == 0)
                offered.Add(role.Name);
        }

        return GenericResult<IReadOnlyList<string>>.Success(offered);
    }
}
