using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the role-mapping domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "RoleMapping")]
public sealed class RoleMappingConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="RoleMappingConfigurationCommand"/> class.</summary>
    public RoleMappingConfigurationCommand() : base("RoleMapping") { }
}
