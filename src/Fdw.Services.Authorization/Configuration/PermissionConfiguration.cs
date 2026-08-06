using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Configuration.Abstractions;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// Database-backed configuration for permissions.
/// Generates the table <c>authz.Permission</c>.
/// </summary>
/// <remarks>
/// <para>
/// Permissions are runtime-configurable through this configuration record (persisted to <c>authz.Permission</c>).
/// The former static <c>Permissions</c> TypeCollection (ConnectionsReadPermission, etc.) has been removed;
/// all permission management now goes through the database-backed path.
/// </para>
/// <para>
/// Each permission is decomposed into one property per parameter:
/// <list type="bullet">
/// <item><description>Domain - the service domain (e.g., "connections", "datastores", "pipelines")</description></item>
/// <item><description>Resource - the specific resource within the domain (e.g., "mssql", "*")</description></item>
/// <item><description>Action - the operation (e.g., "read", "write", "execute")</description></item>
/// <item><description>Scope - the visibility boundary ("tenant", "system", "global")</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Permission")]
public partial class PermissionConfiguration : ConfigurationBase<PermissionConfiguration>
{
    /// <inheritdoc />
    public override string SectionName => "Authorization";

    /// <inheritdoc />
    public override string ServiceType => "Permission";

    // Why: Previously Resource held the full domain (e.g., "connections") and was also used as a
    // grouping key in the UI. Splitting into Domain + Resource allows fine-grained sub-resource
    // permissions (e.g., Domain="connections", Resource="mssql") while keeping domain grouping clean.

    /// <summary>
    /// Gets or sets the service domain this permission applies to (e.g., "connections", "datastores", "pipelines").
    /// Used for UI grouping and policy resolution.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific resource within the domain (e.g., "mssql", "postgresql", "*").
    /// Use "*" to apply to all resources within the domain.
    /// </summary>
    public string Resource { get; set; } = "*";

    /// <summary>
    /// Gets or sets the action this permission grants (e.g., "read", "write", "execute").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    // Why: RequiresTenant was a boolean that couldn't distinguish between system-level and global
    // permissions. Scope is a string column with three valid values: "tenant" (requires tenant context),
    // "system" (system-wide, no tenant), "global" (applies everywhere). This avoids a boolean that
    // collapses two distinct concepts (system vs global) into one false value.

    /// <summary>
    /// Gets or sets the permission scope. Valid values: "tenant" (requires tenant context),
    /// "system" (system-wide administration), "global" (applies in all contexts).
    /// </summary>
    public string Scope { get; set; } = "tenant";

    /// <summary>
    /// Gets or sets the display name for this permission.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the optional description for this permission.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category for UI grouping (e.g., "Data Access", "Administration").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the sort order for UI display within the category.
    /// </summary>
    public int SortOrder { get; set; }
}
