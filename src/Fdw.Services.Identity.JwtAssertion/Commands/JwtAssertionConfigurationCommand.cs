using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.JwtAssertion.Commands;

/// <summary>ConfigurationCommands TypeOption for the JWT-assertion typed body (sec.JwtAssertionIdentity).</summary>
[TypeOption(typeof(ConfigurationCommands), "JwtAssertionIdentity")]
public sealed class JwtAssertionConfigurationCommand : ConfigurationCommandBase<JwtAssertionConfiguration>
{
    /// <inheritdoc/>
    public JwtAssertionConfigurationCommand() : base("JwtAssertionIdentity") { }
}
