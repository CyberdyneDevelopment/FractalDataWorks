using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Routes save and delete for <see cref="JwtTokenManagerConfiguration"/> rows.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "JwtTokenManager")]
public sealed class JwtTokenManagerConfigurationCommand
    : ConfigurationCommandBase<JwtTokenManagerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="JwtTokenManagerConfigurationCommand"/> class.</summary>
    public JwtTokenManagerConfigurationCommand()
        : base("JwtTokenManager")
    {
    }
}
