using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users.Commands;

/// <summary>ConfigurationCommands TypeOption for the User configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "User")]
public sealed class UserConfigurationCommand : ConfigurationCommandBase<UserConfiguration>
{
    /// <inheritdoc/>
    public UserConfigurationCommand() : base("Users") { }
}
