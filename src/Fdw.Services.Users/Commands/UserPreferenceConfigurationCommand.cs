using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Models;

namespace Fdw.Services.Users.Commands;

/// <summary>ConfigurationCommands TypeOption for the UserPreference configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "UserPreference")]
public sealed class UserPreferenceConfigurationCommand : ConfigurationCommandBase<UserPreferencesConfiguration>
{
    /// <inheritdoc/>
    public UserPreferenceConfigurationCommand() : base("UserPreferences") { }
}
