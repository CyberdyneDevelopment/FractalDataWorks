using System;
using System.Collections.Generic;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// What <see cref="IUserPermissions.Get"/> resolved for one user context: the
/// permission strings a token bakes as <c>perm</c> claims, and the role names it bakes as the
/// <c>roles</c> claim role-based checks (<c>ClaimsPrincipal.IsInRole</c>,
/// <c>ISystemRoleConfiguration.IsInRole</c>) read.
/// </summary>
public sealed class EffectiveAuthorization
{
    /// <summary>Initializes a new instance of the <see cref="EffectiveAuthorization"/> class.</summary>
    /// <param name="permissions">The union of all three permission tiers.</param>
    /// <param name="roleNames">The names of every role assigned at the requested tier(s).</param>
    /// <param name="validUntil">
    /// The earliest expiry among the role assignments that granted any of this, or null when none of
    /// them expires. Required rather than optional so that no caller drops it by leaving it out.
    /// </param>
    public EffectiveAuthorization(
        IReadOnlyCollection<string> permissions, IReadOnlyCollection<string> roleNames, DateTimeOffset? validUntil)
    {
        Permissions = permissions;
        RoleNames = roleNames;
        ValidUntil = validUntil;
    }

    /// <summary>The union of all three permission tiers.</summary>
    public IReadOnlyCollection<string> Permissions { get; }

    /// <summary>The names of every role assigned to the user at the requested tier(s).</summary>
    public IReadOnlyCollection<string> RoleNames { get; }

    /// <summary>
    /// The moment this stops being true: the earliest <c>ExpiresAt</c> among the role assignments
    /// that counted. Null when none of them expires.
    /// </summary>
    /// <remarks>
    /// Why it exists (FDW-862): an expired assignment stops counting when permissions are resolved,
    /// but a token baked a minute before the expiry would otherwise carry that role for the rest of
    /// its lifetime. Whoever mints a token from this caps the token's expiry here.
    /// </remarks>
    public DateTimeOffset? ValidUntil { get; }
}
