using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// Database-backed configuration for user-role assignments.
/// Generates the table <c>authz.UserRole</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is a junction table that maps users to roles. A user can have multiple roles,
/// and roles can have multiple users assigned.
/// </para>
/// <para>
/// For tenant-scoped roles, the TenantId must match between the user assignment
/// and the role definition.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "UserRole")]
public partial class UserRoleConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this user-role assignment.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name (format: {UserId}:{RoleId}).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "UserRoles";

    /// <inheritdoc />
    public string ServiceType => "Authorization";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the user identifier (external identity provider ID or internal user ID).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role ID this user is assigned to.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID if this is a tenant-scoped role assignment.
    /// Null for global role assignments.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets who assigned this role (for audit purposes).
    /// </summary>
    public string? AssignedBy { get; set; }

    /// <summary>
    /// Gets or sets when this role was assigned.
    /// </summary>
    public DateTimeOffset? AssignedAt { get; set; }

    /// <summary>
    /// Gets or sets when this role assignment expires (null for permanent).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

}
