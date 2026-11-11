using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the SecretManager domain. Produces IDataCommand
/// instances against the SecretManager configuration table using the base class defaults.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "SecretManager")]
public sealed class SecretManagerConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SecretManagerConfigurationCommand"/> class.</summary>
    public SecretManagerConfigurationCommand() : base("SecretManager") { }
}
