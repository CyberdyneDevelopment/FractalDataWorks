using System;
using System.Collections.Generic;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Represents the identity and authorization context for an incoming request.
/// Resolved from the auth token per-request by middleware.
/// </summary>
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
    bool IsSystemAdmin { get; }
}
