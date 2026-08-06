using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.SecretManagers.MsSql.Configuration;

namespace Fdw.Services.SecretManagers.MsSql.Commands;

/// <summary>ConfigurationCommands TypeOption for the MsSqlSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "MsSqlSecretManager")]
public sealed class MsSqlSecretManagerConfigurationCommand : ConfigurationCommandBase<MsSqlSecretManagerConfiguration>
{
    /// <inheritdoc/>
    public MsSqlSecretManagerConfigurationCommand() : base("MsSqlSecretManager") { }
}
