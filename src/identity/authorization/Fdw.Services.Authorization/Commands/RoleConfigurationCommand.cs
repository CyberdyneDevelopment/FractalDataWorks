using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the Role configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Role")]
public sealed class RoleConfigurationCommand : ConfigurationCommandBase<RoleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="RoleConfigurationCommand"/> class.</summary>
    public RoleConfigurationCommand() : base("Role") { }
}
