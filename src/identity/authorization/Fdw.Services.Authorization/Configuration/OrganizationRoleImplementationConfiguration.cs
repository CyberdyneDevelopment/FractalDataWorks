using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// A role an organization defines for its own members (FDW-1593): a name unique within the organization and the permissions it carries.
/// </summary>
/// <remarks>
/// <para>
/// Persisted to <c>tenant.OrganizationRoleConfiguration</c>, reached through the domain row in <c>tenant.OrganizationRole</c>, whose
/// name is <c>{OrganizationId}:{RoleName}</c> so the same role name can exist in two organizations. The permissions are cascade children.
/// </para>
/// <para>
/// Giving a person the role writes one <c>tenant.TenantOrgAccess</c> row per permission, so a role carries at least one permission:
/// the row-security filters admit a person to an organization by having any current access row for it.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "OrganizationRole")]
public partial class OrganizationRoleImplementationConfiguration : IOrganizationRoleImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid OrganizationId { get; set; }

    /// <inheritdoc />
    public string RoleName { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public bool IsBuiltIn { get; set; }

    /// <inheritdoc />
    public bool IsCurrent { get; set; } = true;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    /// <remarks>Cascade children (<c>tenant.OrganizationRolePermission</c>).</remarks>
    public IList<OrganizationRolePermissionConfiguration> Permissions { get; set; } = [];
}
