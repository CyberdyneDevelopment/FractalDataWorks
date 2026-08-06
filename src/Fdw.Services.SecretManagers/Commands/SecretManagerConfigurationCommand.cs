using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the SecretManager domain. Produces IDataCommand
/// instances against the SecretManager configuration table using the base class defaults.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "SecretManager")]
public sealed class SecretManagerConfigurationCommand : ConfigurationCommandBase<SecretManagerConfiguration>
{
    /// <inheritdoc/>
    public SecretManagerConfigurationCommand() : base("SecretManager") { }
}
