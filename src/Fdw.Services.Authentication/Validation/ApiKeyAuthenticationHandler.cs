using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Validates the opaque credentials this host mints itself — agent keys and personal access tokens.
/// </summary>
/// <remarks>
/// <para>
/// The sibling of the JWT options for credentials that carry no claims of their own. A bearer token
/// states who it is and this host checks the signature; an <c>fdx_</c> credential states nothing —
/// it is a lookup key, and everything the principal knows is read from the row it matches.
/// </para>
/// <para>
/// Which means the identity is BUILT here rather than parsed, and that is why permission claims are
/// resolved in this handler. A JWT arrives with its permissions already baked in at issuance; an
/// opaque credential has no issuance moment to bake them at, so a caller authenticated this way and
/// given no permissions would be authenticated and unable to do anything.
/// </para>
/// </remarks>
internal sealed class ApiKeyAuthenticationHandler : IAuthenticationHandler
{
    private const string BearerPrefix = "Bearer ";

    private readonly IEffectivePermissionResolver _permissions;
    private readonly ILogger _log;

    private AuthenticationScheme? _scheme;
    private HttpContext? _context;

    /// <summary>Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.</summary>
    /// <param name="permissions">Resolves the permissions the credential's owner holds.</param>
    /// <param name="logger">The logger for validation outcomes.</param>
    public ApiKeyAuthenticationHandler(
        IEffectivePermissionResolver permissions,
        ILogger<ApiKeyAuthenticationHandler>? logger = null)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _log = logger ?? NullLogger<ApiKeyAuthenticationHandler>.Instance;
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
        {
            return AuthenticateResult.NoResult();
        }

        var header = _context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith(ApiKeyAuthenticationType.CredentialPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var raw = header[BearerPrefix.Length..].Trim();

        // Told apart by prefix BEFORE validation, never by trying one service and falling back to the
        // other: a fallback reports an unrecognised agent key as a bad token, and whoever reads the
        // log cannot tell which credential actually failed.
        return header.StartsWith(ApiKeyAuthenticationType.AgentKeyPrefix, StringComparison.OrdinalIgnoreCase)
            ? await AuthenticateAgentKey(raw).ConfigureAwait(false)
            : await AuthenticatePersonalAccessToken(raw).ConfigureAwait(false);
    }

    private async Task<AuthenticateResult> AuthenticateAgentKey(string rawKey)
    {
        var service = _context!.RequestServices.GetService<IAgentKeyService>();
        if (service is null)
        {
            AuthenticationValidationLog.CredentialServiceNotRegistered(_log, nameof(IAgentKeyService), "agent key");
            return AuthenticateResult.Fail("Agent key validation is not available.");
        }

        var result = await service.ValidateKey(rawKey, _context.RequestAborted).ConfigureAwait(false);
        if (result.IsFailure)
        {
            AuthenticationValidationLog.OpaqueCredentialRejected(
                _log, "agent key", result.CurrentMessage ?? "validation service error");
            return AuthenticateResult.Fail("Agent key validation failed.");
        }

        if (result.Value is not { IsValid: true } validation)
        {
            AuthenticationValidationLog.OpaqueCredentialRejected(_log, "agent key", "not recognised, revoked or expired");
            return AuthenticateResult.Fail("Agent key is not valid.");
        }

        var permissions = await ResolvePermissions(validation.UserId.ToString()).ConfigureAwait(false);
        if (permissions is null)
        {
            return AuthenticateResult.Fail("Permissions could not be resolved.");
        }

        // The agent claims sit BESIDE sub and never replace it. An agent acts on behalf of its owner,
        // so its sub IS that person's, and every permission check and RLS predicate downstream must
        // keep seeing the person. The claims say who is driving, not who is acting.
        var claims = new List<Claim>
        {
            new(ClaimDefinitions.sub.Name, validation.UserId.ToString()),
            new(ClaimDefinitions.agent.Name, "true"),
            new(ClaimDefinitions.agentLabel.Name, validation.Label),
            new(ClaimDefinitions.agentKeyId.Name, validation.AgentKeyId.ToString(CultureInfo.InvariantCulture)),
        };
        claims.AddRange(permissions.Select(p => new Claim(ClaimDefinitions.perm.Name, p)));

        AuthenticationValidationLog.AgentKeyAccepted(
            _log, validation.Label, validation.UserId.ToString(), permissions.Count);

        return Success(claims);
    }

    private async Task<AuthenticateResult> AuthenticatePersonalAccessToken(string rawToken)
    {
        var service = _context!.RequestServices.GetService<IPersonalAccessTokenService>();
        if (service is null)
        {
            AuthenticationValidationLog.CredentialServiceNotRegistered(
                _log, nameof(IPersonalAccessTokenService), "personal access token");
            return AuthenticateResult.Fail("Personal access token validation is not available.");
        }

        var result = await service.ValidateToken(rawToken, _context.RequestAborted).ConfigureAwait(false);
        if (result.IsFailure)
        {
            AuthenticationValidationLog.OpaqueCredentialRejected(
                _log, "personal access token", result.CurrentMessage ?? "validation service error");
            return AuthenticateResult.Fail("Personal access token validation failed.");
        }

        if (result.Value is not { IsValid: true } validation)
        {
            AuthenticationValidationLog.OpaqueCredentialRejected(
                _log, "personal access token", "not recognised, revoked or expired");
            return AuthenticateResult.Fail("Personal access token is not valid.");
        }

        var permissions = await ResolvePermissions(validation.UserId.ToString()).ConfigureAwait(false);
        if (permissions is null)
        {
            return AuthenticateResult.Fail("Permissions could not be resolved.");
        }

        var claims = new List<Claim> { new(ClaimDefinitions.sub.Name, validation.UserId.ToString()) };
        claims.AddRange(permissions.Select(p => new Claim(ClaimDefinitions.perm.Name, p)));

        AuthenticationValidationLog.PersonalAccessTokenAccepted(_log, validation.UserId.ToString(), permissions.Count);

        return Success(claims);
    }

    /// <summary>Resolves the permissions the credential's owner holds, or null to deny.</summary>
    /// <param name="userId">The owner the credential is bound to.</param>
    /// <remarks>
    /// Tenant and org are passed as null because an opaque credential carries neither — it is a
    /// lookup key, not a token with a context baked into it. Inventing a tenant here would grant
    /// tenant-scoped rows to a caller who never named one.
    ///
    /// A failed resolve denies rather than authenticating with an empty set: the resolver's own
    /// contract says callers must treat failure as deny, and an empty permission set is
    /// indistinguishable from a legitimately unprivileged user.
    /// </remarks>
    private async Task<IReadOnlyCollection<string>?> ResolvePermissions(string userId)
    {
        var resolved = await _permissions
            .Resolve(userId, tenantId: null, orgId: null, isGlobalTenant: false, _context!.RequestAborted)
            .ConfigureAwait(false);

        if (resolved.IsSuccess && resolved.Value is { } permissions)
        {
            return permissions;
        }

        AuthenticationValidationLog.PermissionResolutionFailed(
            _log, userId, resolved.CurrentMessage ?? "no reason given");
        return null;
    }

    private AuthenticateResult Success(IEnumerable<Claim> claims)
        => AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, _scheme!.Name)), _scheme.Name));

    /// <inheritdoc />
    public Task ChallengeAsync(AuthenticationProperties? properties)
    {
        if (_context is not null)
        {
            _context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ForbidAsync(AuthenticationProperties? properties)
    {
        if (_context is not null)
        {
            _context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }

        return Task.CompletedTask;
    }
}
