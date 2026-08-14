using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.SecretManagers.HashiCorpVault.Configuration;

namespace Fdw.Services.SecretManagers.HashiCorpVault.Commands;

/// <summary>ConfigurationCommands TypeOption for the Vault typed body (sec.HashiCorpVaultSecretManager).</summary>
[TypeOption(typeof(ConfigurationCommands), "HashiCorpVaultSecretManager")]
public sealed class HashiCorpVaultConfigurationCommand : ConfigurationCommandBase<HashiCorpVaultConfiguration>
{
    /// <inheritdoc/>
    public HashiCorpVaultConfigurationCommand() : base("HashiCorpVaultSecretManager") { }
}
