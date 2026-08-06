using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Represents a tenant in a multi-tenant system.
/// Tenants are registered via configuration and can have custom themes, options, and role assignments.
/// </summary>
public interface ITenant : ITypeOption<Guid, ITenant>
{
    /// <summary>
    /// Gets the tenant's unique identifier.
    /// </summary>
    new Guid Id { get; }

    /// <summary>
    /// Gets the tenant's slug/code for URLs and configuration lookups.
    /// </summary>
    string Slug { get; }

    /// <summary>
    /// Gets the tenant's organization prefix applied to permission policy names on the
    /// API surface (e.g. "acme" → "acme:connections:read"). Null or empty means no prefix —
    /// permission DTOs return the bare stored name. Stored unprefixed in authz.Permission;
    /// this property is the per-tenant brand applied at the API boundary only.
    /// </summary>
    string? OrgPrefix { get; }

    /// <summary>
    /// Gets whether the tenant is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the tenant's display name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the tenant's theme configuration.
    /// </summary>
    ITenantTheme Theme { get; }

    /// <summary>
    /// Gets the tenant's custom options/settings.
    /// </summary>
    ITenantOptions Options { get; }

    /// <summary>
    /// Gets the roles available to this tenant.
    /// </summary>
    IEnumerable<string> AvailableRoles { get; }

    /// <summary>
    /// Gets whether this tenant is the global/home tenant.
    /// There is exactly one global tenant per deployment (<c>tenant.Tenants.IsGlobal = 1</c>).
    /// Its role grants are included in every user's effective permission set regardless of
    /// which tenant they are currently operating in.
    /// </summary>
    bool IsGlobal { get; }

    /// <summary>
    /// Gets the connection name for tenant-specific data isolation.
    /// </summary>
    string? ConnectionName { get; }

    /// <summary>
    /// Gets the configuration section path for this tenant.
    /// </summary>
    string ConfigurationSection { get; }
}
