using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the AuthChallenge domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "AuthChallenge")]
public sealed class AuthChallengeConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthChallengeConfigurationCommand"/> class.</summary>
    public AuthChallengeConfigurationCommand() : base("AuthChallenge") { }
}
