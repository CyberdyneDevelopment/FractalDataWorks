using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users.Commands;

/// <summary>ConfigurationCommands TypeOption for the users domain's own configuration.</summary>
[TypeOption(typeof(ConfigurationCommands), "UsersService")]
public sealed class UsersServiceConfigurationCommand : ConfigurationCommandBase<UsersServiceConfiguration>
{
    /// <inheritdoc/>
    public UsersServiceConfigurationCommand() : base("UsersService") { }
}
