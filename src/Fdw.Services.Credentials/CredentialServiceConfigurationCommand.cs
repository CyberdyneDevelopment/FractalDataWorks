using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Credentials;

/// <summary>
/// ConfigurationCommands TypeOption for the CredentialService domain. Produces IDataCommand
/// instances against the CredentialService configuration table using the base class defaults.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "CredentialService")]
public sealed class CredentialServiceConfigurationCommand : ConfigurationCommandBase<CredentialServiceConfiguration>
{
    /// <inheritdoc/>
    public CredentialServiceConfigurationCommand() : base("CredentialService") { }
}
