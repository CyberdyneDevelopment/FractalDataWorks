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
public partial class UserTenantImplementationConfiguration
    : IUserTenantImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>The domain record's durable id.</summary>
    public Guid UserTenantsId { get; set; }

    /// <summary>The domain record's row id -- the foreign key the constraint is on.</summary>
    public int UserTenantsRowId { get; set; }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

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
