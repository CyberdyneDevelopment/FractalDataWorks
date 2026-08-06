using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the Permission configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Permission")]
public sealed class PermissionConfigurationCommand : ConfigurationCommandBase<PermissionConfiguration>
{
    /// <inheritdoc/>
    public PermissionConfigurationCommand() : base("Permission") { }
}
