using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Default implementation of <see cref="IRequestContext"/>.
/// Populated by middleware from the authentication context per-request.
/// </summary>
// Why: Replaces ConfigurationScopes (System/User/Merged) with identity-driven visibility.
// The caller's tenant, org membership, and roles determine what they can see —
// not a scope enum that any caller can set to "Merged" and bypass isolation.
public sealed class RequestContext : IRequestContext
{
    /// <summary>
    /// Context for unauthenticated or anonymous requests.
    /// </summary>
    // Why: Static instance avoids allocation on every anonymous request.
    // Empty roles means IsSystemAdmin=false, so no system config access.
    public static readonly RequestContext GuestContext = new(Guid.Empty, [], []);

    /// <summary>
    /// Initializes a new instance of <see cref="RequestContext"/>.
    /// </summary>
    /// <param name="tenantId">The tenant ID for the current request.</param>
    /// <param name="organizationIds">The organization IDs the user belongs to.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    public RequestContext(Guid tenantId, IReadOnlyList<Guid> organizationIds, IReadOnlyList<string> roles)
    {
        TenantId = tenantId;
        OrganizationIds = organizationIds;
        Roles = roles;
    }

    /// <inheritdoc />
    public Guid TenantId { get; }

    /// <inheritdoc />
    public IReadOnlyList<Guid> OrganizationIds { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Roles { get; }

    /// <inheritdoc />
    // Why: Derived from Roles rather than stored separately so it can't diverge
    // from the actual role list. Uses OrdinalIgnoreCase because role names are
    // case-insensitive identifiers, not display strings.
    public bool IsSystemAdmin => Roles.Contains("system-admin", StringComparer.OrdinalIgnoreCase);
}
