using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.JwtAssertion.Commands;

/// <summary>ConfigurationCommands TypeOption for the JWT-assertion typed body (sec.JwtAssertionIdentity).</summary>
[TypeOption(typeof(ConfigurationCommands), "JwtAssertionIdentityConfiguration")]
public sealed class JwtAssertionConfigurationCommand : ConfigurationCommandBase<JwtAssertionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="JwtAssertionConfigurationCommand"/> class.</summary>
    public JwtAssertionConfigurationCommand() : base("JwtAssertionIdentityConfiguration") { }
}
