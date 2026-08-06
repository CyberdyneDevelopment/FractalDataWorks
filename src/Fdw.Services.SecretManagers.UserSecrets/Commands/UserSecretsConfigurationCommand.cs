using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;

namespace Fdw.Services.SecretManagers.UserSecrets.Commands;

/// <summary>ConfigurationCommands TypeOption for the UserSecretsSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "UserSecretsSecretManager")]
public sealed class UserSecretsConfigurationCommand : ConfigurationCommandBase<UserSecretsConfiguration>
{
    /// <inheritdoc/>
    public UserSecretsConfigurationCommand() : base("UserSecretsSecretManager") { }
}
