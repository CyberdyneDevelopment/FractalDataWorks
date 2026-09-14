using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Configuration;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.Services.ExternalIdentityProviders.Results;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// Just-in-time provisioner driven entirely by configured rules: match a claim on the presented
/// subject, create a local account, and grant the mapped roles.
/// </summary>
/// <remarks>
/// <para>
/// No claim type, claim value, role name, or username/email source is a literal anywhere in this
/// class — every one of those is read from <see cref="ClaimMappedProvisioningRuleConfiguration"/>
/// rows. A deployment adding a second auto-provisioning rule (a different claim granting a lesser
/// role, say) never touches this package.
/// </para>
/// <para>
/// Rules are tried in <c>ExecutionOrder</c>; the first whose <c>ClaimType</c>/<c>ClaimValue</c> is
/// present on the subject wins. No matching rule returns <see cref="ProvisionerNotFoundCode"/> — the
/// canonical NOT-FOUND CONTRACT outcome, not a hard failure — so a chain trying several provisioners
/// falls through to the next one, and <c>ResolvePrincipalStepType</c> falls back to its ordinary
/// refusal when this is the only provisioner configured.
/// </para>
/// </remarks>
public sealed class ClaimMappedProvisioner : IExternalIdentityProvisioner
{
    private const string ExternalIdentityDomain = "ExternalIdentity";
    private const string UserRoleDomain = "UserRole";
    private const string UsersDomain = "Users";
    private const string UserTenantsDomain = "UserTenants";

    private readonly ClaimMappedExternalIdentityProvisionerConfiguration _configuration;
    private readonly UserConfigurationProvider _users;
    private readonly UserRoleConfigurationProvider _userRoles;
    private readonly RoleConfigurationProvider _roles;
    private readonly IExternalIdentityConfigurationProvider _identities;
    private readonly UserTenantConfigurationProvider _userTenants;
    private readonly IAuthenticationContextAccessor? _authContextAccessor;
    private readonly ILogger<ClaimMappedProvisioner> _logger;

    /// <summary>Initializes a new instance of the <see cref="ClaimMappedProvisioner"/> class.</summary>
    /// <param name="configuration">This provisioner's own header + rules.</param>
    /// <param name="users">Reads/writes the provisioned account.</param>
    /// <param name="userRoles">Grants a matched rule's mapped roles.</param>
    /// <param name="roles">Resolves a role name to its id.</param>
    /// <param name="identities">Links the provisioned account to the external subject.</param>
    /// <param name="userTenants">Grants tenant access when a matched rule's
    /// <see cref="ClaimMappedProvisioningRuleConfiguration.GrantTenantAccess"/> is set.</param>
    /// <param name="authContextAccessor">Elevates the write context below when available; see the
    /// remarks on <see cref="Provision"/>.</param>
    /// <param name="logger">This provisioner's logger.</param>
    public ClaimMappedProvisioner(
        ClaimMappedExternalIdentityProvisionerConfiguration configuration,
        UserConfigurationProvider users,
        UserRoleConfigurationProvider userRoles,
        RoleConfigurationProvider roles,
        IExternalIdentityConfigurationProvider identities,
        UserTenantConfigurationProvider userTenants,
        IAuthenticationContextAccessor? authContextAccessor,
        ILogger<ClaimMappedProvisioner>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _userRoles = userRoles ?? throw new ArgumentNullException(nameof(userRoles));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _identities = identities ?? throw new ArgumentNullException(nameof(identities));
        _userTenants = userTenants ?? throw new ArgumentNullException(nameof(userTenants));
        _authContextAccessor = authContextAccessor;
        _logger = logger ?? NullLogger<ClaimMappedProvisioner>.Instance;
    }

    // ── IGenericService ────────────────────────────────────────────────────────────

    /// <inheritdoc cref="IPlatformService.Id" />
    public string Id => _configuration.Id.ToString();

    /// <inheritdoc />
    public string Name => _configuration.Name;

    /// <inheritdoc cref="IPlatformService.ServiceType" />
    public string ServiceType => "ClaimMapped";

    /// <inheritdoc cref="IPlatformService.IsAvailable" />
    public bool IsAvailable => true;

    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(
            ExternalIdentityProvisionerLog.CommandNotDispatchable(_logger, command?.CommandType ?? "(null)")));

    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(
            ExternalIdentityProvisionerLog.CommandNotDispatchable(_logger, command?.CommandType ?? "(null)")));

    // ── IExternalIdentityProvisioner ────────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// Runs under <see cref="SystemAuthenticationContextScope"/> when an
    /// <see cref="IAuthenticationContextAccessor"/> was supplied: <see cref="Provision"/> always runs
    /// pre-authentication (FDW consults a provisioner only on an <c>auth.ExternalIdentity</c> lookup
    /// miss — there is never an already-authenticated caller here), so every write below runs under
    /// the RLS deny principal without it. This is a pure widening: a write that already succeeded
    /// under the deny principal (a shared/anonymous-visible row) succeeds identically under elevation;
    /// only a write that was silently blocked by RLS starts succeeding. No previously-working call is
    /// made to fail by elevating. A host with no accessor registered (see the constructor's remarks)
    /// keeps today's un-elevated behavior, unchanged.
    /// </remarks>
    public async Task<IGenericResult<Guid>> Provision(
        string provider,
        string externalSubject,
        ClaimsPrincipal externalPrincipal,
        CancellationToken cancellationToken = default)
    {
        if (_authContextAccessor is null)
            return await ProvisionCore(provider, externalSubject, externalPrincipal, cancellationToken).ConfigureAwait(false);

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);
        return await ProvisionCore(provider, externalSubject, externalPrincipal, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<Guid>> ProvisionCore(
        string provider,
        string externalSubject,
        ClaimsPrincipal externalPrincipal,
        CancellationToken cancellationToken)
    {
        var rule = _configuration.Rules
            .OrderBy(r => r.ExecutionOrder)
            .FirstOrDefault(r => externalPrincipal.Claims.Any(c =>
                string.Equals(c.Type, r.ClaimType, StringComparison.Ordinal)
                && string.Equals(c.Value, r.ClaimValue, StringComparison.Ordinal)));

        if (rule is null)
        {
            ExternalIdentityProvisionerLog.NoRuleMatched(_logger, provider);
            return GenericResult<Guid>.Failure(ExternalIdentityProvisionerResultCodes.ByName("ProvisionerNotFound"));
        }

        if (externalPrincipal.FindFirst(rule.UsernameClaimType)?.Value is not { Length: > 0 } username)
            return GenericResult<Guid>.Failure(
                ExternalIdentityProvisionerLog.RuleMissingUsernameClaim(_logger, rule.Name, rule.UsernameClaimType));

        var email = rule.EmailClaimType is { Length: > 0 } emailClaimType
            ? externalPrincipal.FindFirst(emailClaimType)?.Value
            : null;

        var userId = await CreateOrResumeUser(username, email, rule, cancellationToken).ConfigureAwait(false);
        if (userId.IsFailure)
            return userId;

        var roleNames = rule.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var roleName in roleNames)
        {
            var role = await _roles.Get(roleName, cancellationToken).ConfigureAwait(false);
            if (!role.IsSuccess)
                return role.ToNewResult<Guid>();
            if (role.Value is null)
                return GenericResult<Guid>.Failure(
                    ExternalIdentityProvisionerLog.RuleReferencesUnknownRole(_logger, rule.Name, roleName));

            // {userId}:{roleName} is what the seed writes into authz.UserRole.[Name]; keyed on the
            // role's Id instead, a row written here is invisible to the seed's own idempotence check.
            var grantName = $"{userId.Value}:{role.Value.Name}";
            var grant = await _userRoles.Save(
                new Fdw.Services.Authorization.Configuration.UserRoleImplementationConfiguration
                {
                    Name = grantName,
                    UserId = userId.Value.ToString(),
                    RoleId = role.Value.Id,
                    AssignedBy = Name,
                    AssignedAt = DateTimeOffset.UtcNow,
                },
                UserRoleDomain, UserRoleDomain, grantName, cancellationToken).ConfigureAwait(false);

            if (grant.IsFailure)
                return grant.ToNewResult<Guid>();
        }

        if (rule.GrantTenantAccess)
        {
            // {userId}:{tenantId} mirrors the role grant's own Name convention just above.
            var tenantGrantName = $"{userId.Value}:{rule.TenantId}";
            var tenantGrant = await _userTenants.Save(
                new UserTenantImplementationConfiguration
                {
                    Name = tenantGrantName,
                    UserId = userId.Value,
                    TenantId = rule.TenantId,
                    IsDefault = true,
                },
                UserTenantsDomain, UserTenantsDomain, tenantGrantName, cancellationToken).ConfigureAwait(false);

            if (tenantGrant.IsFailure)
                return tenantGrant.ToNewResult<Guid>();
        }

        // Required by the interface contract: without this row, the NEXT login for the same subject
        // finds no binding either, and provisions a second account instead of resolving to this one.
        var linked = await _identities.Save(new ExternalIdentityConfiguration
        {
            Name = provider,
            Provider = provider,
            ExternalSubject = externalSubject,
            UserId = userId.Value,
            IsActive = true,
        }, ExternalIdentityDomain, ExternalIdentityDomain, provider, cancellationToken).ConfigureAwait(false);

        if (linked.IsFailure)
            return linked.ToNewResult<Guid>();

        ExternalIdentityProvisionerLog.AccountProvisioned(_logger, provider, rule.Name, userId.Value);

        return GenericResult<Guid>.Success(userId.Value);
    }

    /// <summary>
    /// Creates the account this rule provisions, or resumes an interrupted prior attempt.
    /// </summary>
    /// <remarks>
    /// Provisioning writes three rows across two other domains (the user, its role grants, the
    /// identity link) with no cross-command transaction to make that atomic. If role assignment or
    /// the identity link failed on a previous attempt, the user row exists but nothing binds it to
    /// this external subject — every later login reaches this same unbound path again, and
    /// <c>CreateUser</c> would fail with UserAlreadyExists forever, locking the account out with no
    /// recovery. Reaching here at all already means the binding lookup upstream found nothing for
    /// this exact (issuer, subject) pair, so a user existing under this rule's own username is
    /// presumptively that orphan, not a genuine collision with someone else's account — completing
    /// its provisioning is resuming it, not overwriting a stranger.
    /// </remarks>
    private async Task<IGenericResult<Guid>> CreateOrResumeUser(
        string username, string? email, ClaimMappedProvisioningRuleConfiguration rule, CancellationToken cancellationToken)
    {
        // Read first: Save is an upsert by name, so an account that already exists is resumed here
        // rather than written over -- which is what the old "create, then handle UserAlreadyExists"
        // sequence was for.
        var existing = await _users.Get(username, cancellationToken).ConfigureAwait(false);
        if (!existing.IsSuccess)
            return existing.ToNewResult<Guid>();

        if (existing.Value is { } user)
        {
            ExternalIdentityProvisionerLog.ResumingOrphanedUser(_logger, rule.Name, user.Id);
            return GenericResult<Guid>.Success(user.Id);
        }

        var written = await _users.Save(
            new Fdw.Services.Users.Configuration.UserImplementationConfiguration
            {
                Name = username,
                Username = username,
                Email = email,
                TenantId = rule.TenantId,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            UsersDomain, UsersDomain, username, cancellationToken).ConfigureAwait(false);
        if (!written.IsSuccess)
            return written.ToNewResult<Guid>();

        // Save mints the domain row's id inside itself and hands back neither row, so the account
        // is read back by the name it was written under to learn what it was given.
        var provisioned = await _users.Get(username, cancellationToken).ConfigureAwait(false);
        if (!provisioned.IsSuccess)
            return provisioned.ToNewResult<Guid>();
        if (provisioned.Value is null)
            return GenericResult<Guid>.Failure(
                ExternalIdentityProvisionerLog.ProvisionedUserUnreadable(_logger, rule.Name, username));

        return GenericResult<Guid>.Success(provisioned.Value.Id);
    }
}
