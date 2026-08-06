using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// Database-backed configuration for roles.
/// Generates the table <c>authz.Role</c>.
/// </summary>
/// <remarks>
/// <para>
/// Roles are runtime-configurable through this configuration record (persisted to <c>authz.Role</c>).
/// The former static <c>Roles</c> TypeCollection (AdminRole, OperatorRole, etc.) has been removed;
/// all role management now goes through the database-backed path.
/// </para>
/// <para>
/// Use cases:
/// <list type="bullet">
/// <item><description>Custom tenant-specific roles</description></item>
/// <item><description>Dynamic role creation via admin UI</description></item>
/// <item><description>Role hierarchies that can be modified at runtime</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Role")]
public partial class RoleConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "Roles";

    /// <inheritdoc />
    public string ServiceType => "Authorization";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the display name for this role.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the optional description for this role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this role is tenant-scoped.
    /// </summary>
    public bool IsTenantScoped { get; set; } = true;

    /// <summary>
    /// Gets or sets the tenant ID if this is a tenant-specific role.
    /// Null for global roles.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the parent role ID for role inheritance.
    /// Null for top-level roles.
    /// </summary>
    public Guid? ParentRoleId { get; set; }

    /// <summary>
    /// Gets or sets the sort order for UI display.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets when the role was created (audit field — populated from DB).
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Gets or sets when the role was last modified (audit field — populated from DB).
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }
}
