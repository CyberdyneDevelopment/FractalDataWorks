using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Provides access to the current tenant context from HTTP request.
/// </summary>
public interface IRequestTenantInfo
{
    /// <summary>
    /// Gets the current tenant ID from the request context.
    /// Returns null if no tenant is specified (system/admin context).
    /// </summary>
    Guid? CurrentTenantId { get; }

    /// <summary>
    /// Gets whether the current user has access to all tenants (admin).
    /// </summary>
    bool IsSystemAdmin { get; }

    /// <summary>
    /// Gets the current user's username.
    /// </summary>
    string? CurrentUsername { get; }
}
