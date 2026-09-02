using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the system role mapping.</summary>
[TypeOption(typeof(ConfigurationCommands), "SystemRoleMapping")]
public sealed class SystemRoleMappingConfigurationCommand : ConfigurationCommandBase<SystemRoleMappingConfiguration>
{
    /// <inheritdoc/>
    public SystemRoleMappingConfigurationCommand() : base("SystemRoleMapping") { }
}
