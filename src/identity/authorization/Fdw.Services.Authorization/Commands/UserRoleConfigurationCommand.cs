using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the UserRole configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "UserRole")]
public sealed class UserRoleConfigurationCommand : ConfigurationCommandBase<UserRoleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UserRoleConfigurationCommand"/> class.</summary>
    public UserRoleConfigurationCommand() : base("UserRole") { }
}
