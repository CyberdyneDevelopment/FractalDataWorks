using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>ConfigurationCommands TypeOption for the UserRole configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "UserRole")]
public sealed class UserRoleConfigurationCommand : ConfigurationCommandBase<UserRoleConfiguration>
{
    /// <inheritdoc/>
    public UserRoleConfigurationCommand() : base("UserRole") { }
}
