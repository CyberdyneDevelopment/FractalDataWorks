using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the system role mapping.</summary>
[TypeOption(typeof(ConfigurationCommands), "System")]
public sealed class SystemRoleMappingConfigurationCommand : ConfigurationCommandBase<SystemRoleMappingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SystemRoleMappingConfigurationCommand"/> class.</summary>
    public SystemRoleMappingConfigurationCommand() : base("SystemRoleMappingConfiguration") { }
}
