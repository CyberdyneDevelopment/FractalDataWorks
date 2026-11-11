using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Credentials;

/// <summary>
/// ConfigurationCommands TypeOption for the CredentialService domain. Produces IDataCommand
/// instances against the CredentialService configuration table using the base class defaults.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "CredentialService")]
public sealed class CredentialServiceConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CredentialServiceConfigurationCommand"/> class.</summary>
    public CredentialServiceConfigurationCommand() : base("CredentialService") { }
}
