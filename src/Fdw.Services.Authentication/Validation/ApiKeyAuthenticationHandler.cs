using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
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
/// states who it is and this host checks the signature; an agent key or PAT states nothing —
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

    private readonly IUserPermissions _permissions;
    private readonly IAuthenticationServiceConfigurationProvider _configuration;
    private readonly ILogger _log;

    private AuthenticationScheme? _scheme;
    private HttpContext? _context;

    /// <summary>Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.</summary>
    /// <param name="permissions">Resolves the permissions the credential's owner holds.</param>
    /// <param name="configuration">Reads the declared ApiKey service, for the permission each key kind requires.</param>
    /// <param name="logger">The logger for validation outcomes.</param>
    public ApiKeyAuthenticationHandler(
        IUserPermissions permissions,
        IAuthenticationServiceConfigurationProvider configuration,
        ILogger<ApiKeyAuthenticationHandler>? logger = null)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
        if (!ApiKeyAuthenticationType.IsOpaqueCredential(header))
        {
            return AuthenticateResult.NoResult();
        }

        var raw = header[BearerPrefix.Length..].Trim();

        // The agent key service is asked which kind this is because it owns the format: the prefix is
        // its configuration (FDW-939), not this handler's.
        var agentKeys = _context.RequestServices.GetService<IAgentKeyService>();
        if (agentKeys is null)
        {
            AuthenticationValidationLog.CredentialServiceNotRegistered(_log, nameof(IAgentKeyService), "API key");
            return AuthenticateResult.Fail("API key validation is not available.");
        }

        var kind = await agentKeys.IsAgentKey(raw, _context.RequestAborted).ConfigureAwait(false);
        if (kind.IsFailure)
        {
            AuthenticationValidationLog.CredentialKindUnresolved(_log);
            return AuthenticateResult.Fail("API key validation failed.");
        }

        // Told apart by format BEFORE validation, never by trying one service and falling back to the
        // other: a fallback reports an unrecognised agent key as a bad token, and whoever reads the
        // log cannot tell which credential actually failed.
        return kind.Value
            ? await AuthenticateAgentKey(agentKeys, raw).ConfigureAwait(false)
            : await AuthenticatePersonalAccessToken(raw).ConfigureAwait(false);
    }

    private async Task<AuthenticateResult> AuthenticateAgentKey(IAgentKeyService service, string rawKey)
    {
        var result = await service.ValidateKey(rawKey, _context!.RequestAborted).ConfigureAwait(false);
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

        var acting = await ResolveActingAs(validation.UserId).ConfigureAwait(false);
        if (acting is null)
        {
            return AuthenticateResult.Fail("Permissions could not be resolved.");
        }

        if (await Refusal("agent key", c => c.RequiredAgentKeyPermission, validation.UserId, acting.Value.Permissions)
                .ConfigureAwait(false) is { } refused)
        {
            return refused;
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
        claims.AddRange(acting.Value.Claims);

        AuthenticationValidationLog.AgentKeyAccepted(
            _log, validation.Label, validation.UserId.ToString(), acting.Value.Permissions.Count);

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

        var acting = await ResolveActingAs(validation.UserId).ConfigureAwait(false);
        if (acting is null)
        {
            return AuthenticateResult.Fail("Permissions could not be resolved.");
        }

        if (await Refusal("personal access token", c => c.RequiredUserKeyPermission, validation.UserId, acting.Value.Permissions)
                .ConfigureAwait(false) is { } refused)
        {
            return refused;
        }

        var claims = new List<Claim> { new(ClaimDefinitions.sub.Name, validation.UserId.ToString()) };
        claims.AddRange(acting.Value.Claims);

        AuthenticationValidationLog.PersonalAccessTokenAccepted(_log, validation.UserId.ToString(), acting.Value.Permissions.Count);

        return Success(claims);
    }

    /// <summary>
    /// Resolves the tenant the credential's owner acts in and what they may do there, as the claims a
    /// token would carry, or null to deny.
    /// </summary>
    /// <param name="userId">The owner the credential is bound to.</param>
    /// <remarks>
    /// <para>
    /// An opaque credential is a password for scripts: it acts as its owner in the tenant their login
    /// lands in — their default membership — exactly as a password login would, carrying that tenant
    /// and only the roles held in it (FDW-825). It used to act in no tenant at all, which under
    /// per-tenant roles holds no roles and, under row-level security, sees only shared rows.
    /// </para>
    /// <para>
    /// Every failure denies rather than authenticating with less: the permission resolver's own
    /// contract says failure means deny, and an empty permission set is indistinguishable from a
    /// legitimately unprivileged user.
    /// </para>
    /// </remarks>
    private async Task<(IReadOnlyList<Claim> Claims, IReadOnlyCollection<string> Permissions)?> ResolveActingAs(Guid userId)
    {
        var tenantResolver = _context!.RequestServices.GetService<ITenantResolver>();
        var tenantProvider = _context.RequestServices.GetService<ITenantProvider>();
        if (tenantResolver is null || tenantProvider is null)
        {
            AuthenticationValidationLog.CredentialServiceNotRegistered(
                _log, tenantResolver is null ? nameof(ITenantResolver) : nameof(ITenantProvider), "opaque credential tenant");
            return null;
        }

        var tenantId = await tenantResolver.TenantFor(userId, _context.RequestAborted).ConfigureAwait(false);
        if (tenantId.IsFailure)
        {
            AuthenticationValidationLog.PermissionResolutionFailed(
                _log, userId.ToString(), tenantId.CurrentMessage ?? "no reason given");
            return null;
        }

        var tenant = await tenantProvider.GetTenant(tenantId.Value, _context.RequestAborted).ConfigureAwait(false);
        if (tenant.IsFailure || tenant.Value is null)
        {
            AuthenticationValidationLog.PermissionResolutionFailed(
                _log, userId.ToString(), tenant.CurrentMessage ?? "the tenant could not be read");
            return null;
        }

        var resolved = await _permissions
            .Get(userId.ToString(), tenantId.Value, orgId: null, _context.RequestAborted)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess || resolved.Value is not { } authorization)
        {
            AuthenticationValidationLog.PermissionResolutionFailed(
                _log, userId.ToString(), resolved.CurrentMessage ?? "no reason given");
            return null;
        }

        var claims = new List<Claim> { new(ClaimDefinitions.tenantId.Name, tenantId.Value.ToString()) };
        if (tenant.Value.IsGlobal)
        {
            claims.Add(new Claim(ClaimDefinitions.globalTenant.Name, "true"));
        }

        claims.AddRange(authorization.Permissions.Select(p => new Claim(ClaimDefinitions.perm.Name, p)));
        claims.AddRange(authorization.RoleNames.Select(r => new Claim(ClaimDefinitions.roles.Name, r)));

        // The standard role claim beside `roles`, not instead of it: the layered settings resolve a role's override from
        // ClaimTypes.Role, so a key whose roles sit only under `roles` would never reach its role-level settings (FDW-1209).
        // Every reader that takes `roles` still finds it, and the principal-to-context mapping collects both distinctly.
        claims.AddRange(authorization.RoleNames.Select(r => new Claim(ClaimTypes.Role, r)));

        return (claims, authorization.Permissions);
    }

    /// <summary>
    /// Refuses the credential when this service requires a permission of its kind and the owner does
    /// not hold it in the tenant the credential acts in; null when the credential may authenticate.
    /// </summary>
    /// <param name="kind">The credential kind, for the log.</param>
    /// <param name="required">Which of the row's requirements applies to this kind.</param>
    /// <param name="userId">The owner.</param>
    /// <param name="held">What the owner holds there, already resolved.</param>
    /// <remarks>
    /// The same gate a sign-in flow applies, so an operator deciding who may use API keys decides it
    /// once per kind, and a key sees exactly what its owner would see signed in. A row that cannot be
    /// read refuses: the requirement might be there, and authenticating without knowing is the one
    /// answer that could be wrong in the unsafe direction.
    /// </remarks>
    private async Task<AuthenticateResult?> Refusal(
        string kind, Func<IApiKeyAuthenticationConfiguration, string?> required, Guid userId, IReadOnlyCollection<string> held)
    {
        var serviceName = _scheme!.Name[ApiKeyAuthenticationType.SchemePrefix.Length..];
        var entry = await _configuration.Get(serviceName, _context!.RequestAborted).ConfigureAwait(false);
        if (entry.IsFailure || entry.Value is not IApiKeyAuthenticationConfiguration service)
        {
            AuthenticationValidationLog.ApiKeyServiceUnreadable(
                _log, serviceName, entry.CurrentMessage ?? "its row is not an ApiKey implementation");
            return AuthenticateResult.Fail("The API key service could not be read.");
        }

        if (required(service) is not { Length: > 0 } permission
            || held.Contains(permission, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        AuthenticationValidationLog.ApiKeyNotPermitted(_log, kind, userId.ToString(), permission);
        return AuthenticateResult.Fail("The owner of this credential may not use it.");
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
