using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// One permission a role an organization defines carries (FDW-1593).
/// </summary>
/// <remarks>Cascade child of the role, in <c>tenant.OrganizationRolePermission</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "OrganizationRole")]
public sealed partial class OrganizationRolePermissionConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the permission's name, as the permission catalogue names it (for example <c>datasets:read</c>).</summary>
    public string PermissionName { get; set; } = string.Empty;
}
