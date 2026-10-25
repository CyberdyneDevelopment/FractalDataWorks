using System;
using System.Globalization;
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
/// A thing in a tenant the caller does not act in is refused. The system context passes, as for the grant rule. Nothing here assumes a
/// tenant: a scope with no tenant is refused.
/// </para>
/// </remarks>
public sealed class HierarchyScopeAuthority : IScopeAuthority
{
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IUserPermissions _permissions;
    private readonly ILogger<HierarchyScopeAuthority> _logger;

    /// <summary>Initializes a new instance of the <see cref="HierarchyScopeAuthority"/> class.</summary>
    /// <param name="authContext">Supplies the caller, the tenant they act in and the rights of their session.</param>
    /// <param name="permissions">Reads the rights a person holds in a tenant and, with an organization, in that organization.</param>
    /// <param name="logger">The logger.</param>
    public HierarchyScopeAuthority(
        IAuthenticationContextAccessor authContext, IUserPermissions permissions, ILogger<HierarchyScopeAuthority>? logger)
    {
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
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

        if (caller.ActiveTenantId != scope.TenantId)
            return GenericResult.Failure(ScopeAuthorityLog.OtherTenant(_logger, right, where));

        if (!PermissionCoverage.Covers(caller.Permissions, right))
            return GenericResult.Failure(ScopeAuthorityLog.SessionLacksRight(_logger, right, where));

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
