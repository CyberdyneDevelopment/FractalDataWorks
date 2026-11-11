using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Models;

namespace Fdw.Services.Users.Commands;

/// <summary>ConfigurationCommands TypeOption for the UserPreference configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "UserPreference")]
public sealed class UserPreferenceConfigurationCommand : ConfigurationCommandBase<UserPreferencesImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UserPreferenceConfigurationCommand"/> class.</summary>
    public UserPreferenceConfigurationCommand() : base("UserPreferences") { }
}
