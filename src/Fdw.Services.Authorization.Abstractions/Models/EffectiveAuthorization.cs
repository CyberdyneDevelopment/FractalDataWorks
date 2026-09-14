using System.Collections.Generic;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// What <see cref="IEffectivePermissionResolver.Resolve"/> resolved for one user context: the
/// permission strings a token bakes as <c>perm</c> claims, and the role names it bakes as the
/// <c>roles</c> claim role-based checks (<c>ClaimsPrincipal.IsInRole</c>,
/// <c>ISystemRoleConfiguration.IsInRole</c>) read.
/// </summary>
public sealed class EffectiveAuthorization
{
    /// <summary>Initializes a new instance of the <see cref="EffectiveAuthorization"/> class.</summary>
    public EffectiveAuthorization(IReadOnlyCollection<string> permissions, IReadOnlyCollection<string> roleNames)
    {
        Permissions = permissions;
        RoleNames = roleNames;
    }

    /// <summary>The union of all three permission tiers.</summary>
    public IReadOnlyCollection<string> Permissions { get; }

    /// <summary>The names of every role assigned to the user at the requested tier(s).</summary>
    public IReadOnlyCollection<string> RoleNames { get; }
}
