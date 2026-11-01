using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the permissions of a role an organization defines. Targets the
/// <c>tenant.OrganizationRolePermission</c> table (FDW-1593).
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="OrganizationRolePermissionConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a role.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "OrganizationRolePermission")]
public sealed class OrganizationRolePermissionConfigurationCommand : ConfigurationCommandBase<OrganizationRolePermissionConfiguration>
{
    /// <summary>Initializes the command targeting the OrganizationRolePermission table.</summary>
    public OrganizationRolePermissionConfigurationCommand() : base("OrganizationRolePermission") { }
}
