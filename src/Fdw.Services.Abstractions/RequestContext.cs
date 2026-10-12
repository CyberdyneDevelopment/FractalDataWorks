using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Default implementation of <see cref="IRequestContext"/>.
/// Populated by middleware from the authentication context per-request.
/// </summary>
public sealed class RequestContext : IRequestContext
{
    /// <summary>
    /// Context for unauthenticated or anonymous requests: no tenant, no roles, not an admin.
    /// </summary>
    public static readonly RequestContext GuestContext = new(tenantId: null, [], [], isPlatformAdmin: false);

    /// <summary>
    /// Initializes a new instance of <see cref="RequestContext"/>.
    /// </summary>
    /// <param name="tenantId">The tenant ID for the current request.</param>
    /// <param name="organizationIds">The organization IDs the user belongs to.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <param name="isPlatformAdmin">Whether the caller administers the platform.</param>
    public RequestContext(Guid? tenantId, IReadOnlyList<Guid> organizationIds, IReadOnlyList<string> roles, bool isPlatformAdmin)
    {
        TenantId = tenantId;
        OrganizationIds = organizationIds;
        Roles = roles;
        IsPlatformAdmin = isPlatformAdmin;
    }

    /// <inheritdoc />
    public Guid? TenantId { get; }

    /// <inheritdoc />
    public IReadOnlyList<Guid> OrganizationIds { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Roles { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Stated by whoever builds the context rather than inferred from <see cref="Roles"/>: a role
    /// name alone cannot say whether it is held in the global tenant. This used to test for a literal
    /// "system-admin" role that nothing grants, so it was false for everyone.
    /// </remarks>
    public bool IsPlatformAdmin { get; }
}
