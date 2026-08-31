using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Authentication.Validation;

namespace Fdw.Services.Authentication.Commands;

/// <summary>
/// Reads and writes <c>auth.AuthenticationService</c> rows.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "AuthenticationService")]
public sealed class AuthenticationServiceConfigurationCommand
    : ConfigurationCommandBase<AuthenticationServiceConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationServiceConfigurationCommand"/> class.</summary>
    public AuthenticationServiceConfigurationCommand()
        : base("AuthenticationService")
    {
    }
}
