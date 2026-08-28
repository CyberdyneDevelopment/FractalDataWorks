using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Users.Configuration;

/// <summary>
/// Database-backed configuration for user-tenant memberships.
/// Maps to <c>tenant.UserTenants</c> in ConfigurationDb.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "UserTenant")]
public partial class UserTenantConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "UserTenants";

    /// <inheritdoc />
    public string ServiceType => "UserTenant";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    // ── Domain columns ──

    /// <summary>Gets or sets the user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the tenant identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets whether this is the user's default (preferred) tenant.</summary>
    public bool IsDefault { get; set; }

    // ── Soft-delete / version-on-write state ──

    /// <summary>Gets or sets whether this row is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this row is soft-deleted.</summary>
    public bool IsDeleted { get; set; }
}
