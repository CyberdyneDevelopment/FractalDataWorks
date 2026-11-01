using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// The default <see cref="IScopeAuthority"/>: a right held at the thing's scope or at a scope above it counts (FDW-2327).
/// </summary>
/// <remarks>
/// <para>
/// A caller holds a right at a scope through the roles they hold in the tenant, which reach every organization of it, and through what an
/// organization grants, which reaches that organization only. The rights are read for the scope asked about
/// (<see cref="IUserPermissions"/> with the thing's tenant and organization), not taken from the token, because the token carries the
/// tiers of the organization the caller acts in and a thing may be in another.
/// </para>
/// <para>
/// The session must also hold the right. A caller acting as a role (FDW-2282) holds that role's rights only, so a narrower session is
/// not widened by what the person holds in their own right. Both must hold, and a refusal says which did not.
/// </para>
/// <para>
/// A platform administrator is recognised as the path policies recognise one (FDW-2852): the caller holds the admin role while acting in
/// the global tenant (FDW-825). Such a caller holds the right at every scope of every tenant, so they need no role in the tenant and may
/// reach a tenant they do not act in; their session must still hold the right, and the allowance is logged. A caller who switched into a
/// tenant carries only the roles held there and is not a platform administrator, so they are judged by the roles they hold in it.
/// </para>
/// <para>
/// A thing in a tenant the caller does not act in is refused, other than for a platform administrator. The system context passes, as for the
/// grant rule. Nothing here assumes a tenant: a scope with no tenant is refused.
/// </para>
/// </remarks>
public sealed class HierarchyScopeAuthority : IScopeAuthority
{
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IUserPermissions _permissions;
    private readonly ISystemRoleConfiguration _systemRoles;
    private readonly ILogger<HierarchyScopeAuthority> _logger;

    /// <summary>Initializes a new instance of the <see cref="HierarchyScopeAuthority"/> class.</summary>
    /// <param name="authContext">Supplies the caller, the tenant they act in and the rights of their session.</param>
    /// <param name="permissions">Reads the rights a person holds in a tenant and, with an organization, in that organization.</param>
    /// <param name="systemRoles">Names the admin role, which a caller holds in the global tenant to be the platform's administrator.</param>
    /// <param name="logger">The logger.</param>
    public HierarchyScopeAuthority(
        IAuthenticationContextAccessor authContext, IUserPermissions permissions, ISystemRoleConfiguration systemRoles, ILogger<HierarchyScopeAuthority>? logger)
    {
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _systemRoles = systemRoles ?? throw new ArgumentNullException(nameof(systemRoles));
        _logger = logger ?? NullLogger<HierarchyScopeAuthority>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> May(string right, ThingScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(right);
        ArgumentNullException.ThrowIfNull(scope);

        if (scope.TenantId == Guid.Empty)
            return GenericResult.Failure(ScopeAuthorityLog.NoScope(_logger, right));

        var where = Describe(scope);
        if (_authContext.Current is not { IsAuthenticated: true } caller)
            return GenericResult.Failure(ScopeAuthorityLog.CallerRequired(_logger, right, where));

        if (caller.IsSystemContext)
            return GenericResult.Success();

        var administersPlatform = AdministersPlatform(caller);
        var refusal = SessionRefusal(caller, right, scope, where, administersPlatform);
        if (refusal is not null)
            return GenericResult.Failure(refusal);

        if (administersPlatform)
        {
            ScopeAuthorityLog.AllowedPlatformAdministrator(_logger, right, where);
            return GenericResult.Success();
        }

        return await HeldAtScope(caller, right, scope, where, cancellationToken).ConfigureAwait(false);
    }

    // The framework's own definition of a platform administrator (ISystemRoleConfiguration.IsPlatformAdmin, FDW-825) read from the
    // authentication context: the admin role, held while acting in the global tenant, which only a token for that tenant carries.
    private bool AdministersPlatform(IAuthenticationContext caller)
        => caller.Roles.Contains(_systemRoles.AdminRoleName, StringComparer.OrdinalIgnoreCase)
           && caller.Claims.TryGetValue(ClaimDefinitions.globalTenant.Name, out var claim)
           && string.Equals(Convert.ToString(claim, CultureInfo.InvariantCulture), "true", StringComparison.OrdinalIgnoreCase);

    // What the session itself rules out: a tenant it does not act in (a platform administrator reaches any), or a right it does not carry.
    private IGenericMessage? SessionRefusal(IAuthenticationContext caller, string right, ThingScope scope, string where, bool administersPlatform)
    {
        if (!administersPlatform && caller.ActiveTenantId != scope.TenantId)
            return ScopeAuthorityLog.OtherTenant(_logger, right, where);

        return PermissionCoverage.Covers(caller.Permissions, right)
            ? null
            : ScopeAuthorityLog.SessionLacksRight(_logger, right, where);
    }

    private async Task<IGenericResult> HeldAtScope(
        IAuthenticationContext caller, string right, ThingScope scope, string where, CancellationToken cancellationToken)
    {
        var held = await _permissions.Get(caller.UserId, scope.TenantId, scope.OrganizationId, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure)
            return held;

        if (!PermissionCoverage.Covers(held.Value!.Permissions, right))
            return GenericResult.Failure(ScopeAuthorityLog.RightNotHeldAtScope(_logger, right, where));

        ScopeAuthorityLog.Allowed(_logger, right, where);
        return GenericResult.Success();
    }

    private static string Describe(ThingScope scope) => scope.OrganizationId is { } organization
        ? string.Create(CultureInfo.InvariantCulture, $"organization '{organization:D}' of tenant '{scope.TenantId:D}'")
        : string.Create(CultureInfo.InvariantCulture, $"tenant '{scope.TenantId:D}'");
}
