using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the Permission configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Permission")]
public sealed class PermissionConfigurationCommand : ConfigurationCommandBase<PermissionImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PermissionConfigurationCommand"/> class.</summary>
    public PermissionConfigurationCommand() : base("Permission") { }
}
