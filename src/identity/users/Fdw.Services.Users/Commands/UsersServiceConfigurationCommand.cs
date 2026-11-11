using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users.Commands;

/// <summary>ConfigurationCommands TypeOption for the users domain's own configuration.</summary>
[TypeOption(typeof(ConfigurationCommands), "UsersService")]
public sealed class UsersServiceConfigurationCommand : ConfigurationCommandBase<UsersServiceImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UsersServiceConfigurationCommand"/> class.</summary>
    public UsersServiceConfigurationCommand() : base("UsersService") { }
}
