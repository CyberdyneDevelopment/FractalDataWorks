using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;

namespace Fdw.Services.SecretManagers.EnvironmentVariable.Commands;

/// <summary>ConfigurationCommands TypeOption for the EnvironmentVariableSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "EnvironmentVariableSecretManager")]
public sealed class EnvironmentVariableConfigurationCommand : ConfigurationCommandBase<EnvironmentVariableConfiguration>
{
    /// <inheritdoc/>
    public EnvironmentVariableConfigurationCommand() : base("EnvironmentVariableSecretManager") { }
}
