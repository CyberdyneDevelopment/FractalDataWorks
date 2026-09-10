using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the role-mapping domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "RoleMapping")]
public sealed class RoleMappingConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public RoleMappingConfigurationCommand() : base("RoleMapping") { }
}
