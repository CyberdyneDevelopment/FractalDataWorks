using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.Authentik.Commands;

/// <summary>ConfigurationCommands TypeOption for the Authentik federated-JWT typed body (sec.AuthentikJwtFederationIdentity).</summary>
[TypeOption(typeof(ConfigurationCommands), "AuthentikJwtFederationIdentity")]
public sealed class AuthentikJwtFederationConfigurationCommand : ConfigurationCommandBase<AuthentikJwtFederationConfiguration>
{
    /// <inheritdoc/>
    public AuthentikJwtFederationConfigurationCommand() : base("AuthentikJwtFederationIdentity") { }
}
