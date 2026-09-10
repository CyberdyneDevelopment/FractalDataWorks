using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the AuthChallenge domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "AuthChallenge")]
public sealed class AuthChallengeConfigurationCommand : ConfigurationCommandBase<AuthChallengeConfiguration>
{
    /// <inheritdoc/>
    public AuthChallengeConfigurationCommand() : base("AuthChallenge") { }
}
