using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the RolePermission configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "RolePermission")]
public sealed class RolePermissionConfigurationCommand : ConfigurationCommandBase<RolePermissionConfiguration>
{
    /// <inheritdoc/>
    public RolePermissionConfigurationCommand() : base("RolePermission") { }
}
