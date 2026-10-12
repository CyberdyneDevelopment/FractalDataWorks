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
    /// Gets the tenant the caller is acting in — the tenant their token was issued for — or
    /// <see langword="null"/> when the request acts in none (anonymous, or a token that names no
    /// tenant). Never <see cref="Guid.Empty"/> standing in for that absence (FDW-825).
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets the organization IDs the current user is a member of.
    /// </summary>
    IReadOnlyList<Guid> OrganizationIds { get; }

    /// <summary>
    /// Gets the roles assigned to the current user.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Gets whether the caller administers the PLATFORM — the admin role held while acting in the
    /// global tenant, the same rule as <c>ISystemRoleConfiguration.IsPlatformAdmin</c>. An admin of
    /// any other tenant is not (FDW-825).
    /// </summary>
    bool IsPlatformAdmin { get; }
}
