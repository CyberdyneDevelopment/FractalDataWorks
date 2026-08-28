using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Reads and writes <c>auth.AuthenticationFlowStep</c>.</summary>
[TypeOption(typeof(ConfigurationCommands), "AuthenticationFlowStep")]
public sealed class AuthenticationFlowStepConfigurationCommand
    : ConfigurationCommandBase<AuthenticationFlowStepConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowStepConfigurationCommand"/> class.</summary>
    public AuthenticationFlowStepConfigurationCommand() : base("AuthenticationFlowStep") { }
}
