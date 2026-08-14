using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.Authentik.Commands;

/// <summary>ConfigurationCommands TypeOption for the Authentik client-credentials typed body (sec.AuthentikClientCredentialsIdentity).</summary>
[TypeOption(typeof(ConfigurationCommands), "AuthentikClientCredentialsIdentity")]
public sealed class AuthentikClientCredentialsConfigurationCommand : ConfigurationCommandBase<AuthentikClientCredentialsConfiguration>
{
    /// <inheritdoc/>
    public AuthentikClientCredentialsConfigurationCommand() : base("AuthentikClientCredentialsIdentity") { }
}
