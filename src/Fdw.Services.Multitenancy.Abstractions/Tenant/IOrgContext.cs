using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Provides access to the current organization context.
/// Scoped per-request. Resolved after tenant context so the default-org fallback can
/// use the tenant provider to look up <c>IsDefault=1</c> for the current tenant.
/// </summary>
public interface IOrgContext
{
    /// <summary>
    /// Gets the current organization configuration, or null if not resolved.
    /// </summary>
    OrganizationConfiguration? CurrentOrg { get; }

    /// <summary>
    /// Gets the current organization identifier, or null if not resolved.
    /// </summary>
    Guid? OrgId { get; }

    /// <summary>
    /// Gets whether an org context is active.
    /// </summary>
    bool HasOrg { get; }
}
