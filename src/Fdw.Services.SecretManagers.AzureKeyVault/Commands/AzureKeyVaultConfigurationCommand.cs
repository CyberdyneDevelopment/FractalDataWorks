using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;

namespace Fdw.Services.SecretManagers.AzureKeyVault.Commands;

/// <summary>ConfigurationCommands TypeOption for the AzureKeyVaultSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "AzureKeyVaultSecretManager")]
public sealed class AzureKeyVaultConfigurationCommand : ConfigurationCommandBase<AzureKeyVaultConfiguration>
{
    /// <inheritdoc/>
    public AzureKeyVaultConfigurationCommand() : base("AzureKeyVaultSecretManager") { }
}
