using System;
using System.Collections.Generic;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Represents the identity and authorization context for an incoming request.
/// Resolved from the auth token per-request by middleware.
/// </summary>
// Why: Replaces ConfigurationScopes (System/User/Merged) with a richer context.
// Visibility is driven by who is asking (tenant, org membership, roles), not by a scope enum.
public interface IRequestContext
{
    /// <summary>
    /// Gets the tenant ID for the current request.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Gets the organization IDs the current user is a member of.
    /// </summary>
    IReadOnlyList<Guid> OrganizationIds { get; }

    /// <summary>
    /// Gets the roles assigned to the current user.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Gets whether the current user has the system-admin role.
    /// </summary>
    // Why: Derived from Roles rather than stored separately, so it cannot drift from the
    // role set the token actually carries. Consumed by AdminOnlyPolicy and TenantScopedPolicy.
    bool IsSystemAdmin { get; }
}
