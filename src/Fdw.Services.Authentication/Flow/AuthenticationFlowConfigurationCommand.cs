using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Reads and writes <c>auth.AuthenticationFlow</c>.</summary>
[TypeOption(typeof(ConfigurationCommands), "AuthenticationFlow")]
public sealed class AuthenticationFlowConfigurationCommand
    : ConfigurationCommandBase<AuthenticationFlowConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowConfigurationCommand"/> class.</summary>
    public AuthenticationFlowConfigurationCommand() : base("AuthenticationFlow") { }
}
