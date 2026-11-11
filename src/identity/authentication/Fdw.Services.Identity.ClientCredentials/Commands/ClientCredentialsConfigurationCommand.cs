using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.ClientCredentials.Commands;

/// <summary>ConfigurationCommands TypeOption for the client-credentials typed body (sec.ClientCredentialsIdentity).</summary>
[TypeOption(typeof(ConfigurationCommands), "ClientCredentialsIdentityConfiguration")]
public sealed class ClientCredentialsConfigurationCommand : ConfigurationCommandBase<ClientCredentialsConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ClientCredentialsConfigurationCommand"/> class.</summary>
    public ClientCredentialsConfigurationCommand() : base("ClientCredentialsIdentityConfiguration") { }
}
