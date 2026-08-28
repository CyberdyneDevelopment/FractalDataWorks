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
    /// Context for unauthenticated or anonymous requests.
    /// </summary>
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
    public bool IsSystemAdmin => Roles.Contains("system-admin", StringComparer.OrdinalIgnoreCase);
}
