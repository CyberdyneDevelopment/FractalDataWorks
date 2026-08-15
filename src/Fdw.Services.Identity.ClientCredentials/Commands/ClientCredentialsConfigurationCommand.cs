using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.ClientCredentials.Commands;

/// <summary>ConfigurationCommands TypeOption for the client-credentials typed body (sec.ClientCredentialsIdentity).</summary>
[TypeOption(typeof(ConfigurationCommands), "ClientCredentialsIdentity")]
public sealed class ClientCredentialsConfigurationCommand : ConfigurationCommandBase<ClientCredentialsConfiguration>
{
    /// <inheritdoc/>
    public ClientCredentialsConfigurationCommand() : base("ClientCredentialsIdentity") { }
}
