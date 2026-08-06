using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// Database-backed configuration for role-permission assignments.
/// Generates the table <c>authz.RolePermission</c> as a child of <c>authz.Role</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is a junction table that maps roles to permissions. A role can have multiple permissions,
/// and permissions can be assigned to multiple roles.
/// </para>
/// <para>
/// When a role is deleted, its permission assignments are cascade-deleted via the FK relationship.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "RolePermission")]
public partial class RolePermissionConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this role-permission assignment.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name (format: {RoleId}:{PermissionId}).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "RolePermissions";

    /// <inheritdoc />
    public string ServiceType => "Authorization";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the role ID this permission is assigned to.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Gets or sets the permission ID assigned to this role.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Gets or sets additional grant conditions (e.g., resource scope, time restrictions).
    /// </summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// Gets or sets who assigned this permission to the role (for audit purposes).
    /// </summary>
    public string? AssignedBy { get; set; }

    /// <summary>
    /// Gets or sets when this permission was assigned to the role.
    /// </summary>
    public DateTimeOffset? AssignedAt { get; set; }

}
