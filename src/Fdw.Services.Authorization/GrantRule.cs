using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// The grant rule: a caller can give or define only what they hold themselves, only if they also hold the right to modify it, and only in
/// the tenant they act in.
/// </summary>
/// <remarks>
/// What a role carries is read through <see cref="IRoleGrants"/>. What the caller holds is the permissions of their own
/// authentication context. A held <c>resource:*</c> or <c>*:*</c> counts for every permission it covers, as the permission
/// checker treats them. The right to modify a permission is the permission <c>resource:delegate</c> for its resource (FDW-1867): a caller
/// needs it as well as the permission, held through any role that carries it. A carried <c>*:*</c> needs <c>*:*</c> held. The system
/// context passes: it acts for the platform and holds no user's permissions.
/// </remarks>
public sealed class GrantRule : IGrantRule
{
    private readonly IRoleGrants _roleGrants;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly ILogger<GrantRule> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrantRule"/> class.
    /// </summary>
    /// <param name="roleGrants">Expands a role's name into the permissions it carries.</param>
    /// <param name="authContext">Supplies the caller, whose permissions and tenant the rule compares against.</param>
    /// <param name="logger">The logger.</param>
    public GrantRule(IRoleGrants roleGrants, IAuthenticationContextAccessor authContext, ILogger<GrantRule>? logger)
    {
        _roleGrants = roleGrants ?? throw new ArgumentNullException(nameof(roleGrants));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _logger = logger ?? NullLogger<GrantRule>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult> CanGive(IReadOnlyList<string> roleNames, Guid tenantId, CancellationToken cancellationToken = default)
        => Judge(roleNames, tenantId, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult> CanGiveInNewTenant(IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default)
        => Judge(roleNames, null, cancellationToken);

    // A null tenant is a tenant being made: it is never the one the caller acts in, so only what the caller holds is compared.
    private async Task<IGenericResult> Judge(IReadOnlyList<string> roleNames, Guid? tenantId, CancellationToken cancellationToken)
    {
        if (roleNames is null || roleNames.Count == 0)
            return GenericResult.Failure(GrantRuleLog.NothingNamed(_logger));

        // Read before the expansion below: IRoleGrants runs under the system context and restores this one afterwards.
        if (_authContext.Current is not { IsAuthenticated: true } caller)
            return GenericResult.Failure(GrantRuleLog.CallerRequired(_logger));

        if (caller.IsSystemContext)
            return GenericResult.Success();

        if (tenantId is { } actingIn && caller.ActiveTenantId != actingIn)
            return GenericResult.Failure(GrantRuleLog.OtherTenant(_logger, actingIn.ToString()));

        var carried = await _roleGrants.Get(roleNames, cancellationToken).ConfigureAwait(false);
        if (!carried.IsSuccess)
            return carried;

        return carried.Value is { } permissions
            ? Decide(caller, permissions, tenantId, string.Join(", ", roleNames))
            : GenericResult.Failure(AuthorizationLog.RoleProviderQueryFailed(_logger));
    }

    /// <inheritdoc />
    public IGenericResult CanDefine(IReadOnlyList<string> permissionNames, Guid tenantId)
    {
        if (permissionNames is null || permissionNames.Count == 0)
            return GenericResult.Failure(GrantRuleLog.NothingNamed(_logger));

        if (_authContext.Current is not { IsAuthenticated: true } caller)
            return GenericResult.Failure(GrantRuleLog.CallerRequired(_logger));

        if (caller.IsSystemContext)
            return GenericResult.Success();

        return caller.ActiveTenantId == tenantId
            ? Decide(caller, permissionNames, tenantId, "the role")
            : GenericResult.Failure(GrantRuleLog.OtherTenant(_logger, tenantId.ToString()));
    }

    private IGenericResult Decide(IAuthenticationContext caller, IEnumerable<string> carried, Guid? tenantId, string what)
    {
        var held = caller.Permissions.ToList();
        var carriedList = carried.ToList();
        var missing = carriedList.Where(permission => !Holds(held, permission)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count > 0)
            return GenericResult.Failure(GrantRuleLog.RightsNotHeld(_logger, string.Join(", ", missing)));

        // Holding what a role carries is not enough: the caller also needs the right to modify it, a role that carries resource:delegate (FDW-1867).
        var unmodifiable = carriedList.Select(ModifyRight).Distinct(StringComparer.OrdinalIgnoreCase).Where(right => !Holds(held, right)).ToList();
        if (unmodifiable.Count > 0)
            return GenericResult.Failure(GrantRuleLog.ModifyRightNotHeld(_logger, string.Join(", ", unmodifiable)));

        GrantRuleLog.Allowed(_logger, tenantId is { } id ? id.ToString() : "a new tenant", what);
        return GenericResult.Success();
    }

    // The right to modify a permission: delegate on its resource; for a wildcard of every resource, the wildcard itself.
    private static string ModifyRight(string permission)
    {
        var colon = permission.IndexOf(':', StringComparison.Ordinal);
        var resource = colon < 0 ? permission : permission[..colon];
        return string.Equals(resource, "*", StringComparison.Ordinal) ? "*:*" : resource + ":delegate";
    }

    // A held permission covers a permission when it is the same, or is resource:* for its resource, or is *:*.
    private static bool Holds(IReadOnlyList<string> held, string permission)
        => held.Any(have => string.Equals(have, permission, StringComparison.OrdinalIgnoreCase)
            || string.Equals(have, "*:*", StringComparison.Ordinal)
            || (have.EndsWith(":*", StringComparison.Ordinal)
                && permission.StartsWith(have[..^1], StringComparison.OrdinalIgnoreCase)));
}
