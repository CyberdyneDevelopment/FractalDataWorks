using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.TokenManagers.Commands;

/// <summary>ConfigurationCommands TypeOption for the TokenManager configuration domain (auth.TokenManager).</summary>
[TypeOption(typeof(ConfigurationCommands), "TokenManager")]
public sealed class TokenManagerConfigurationCommand : ConfigurationCommandBase<TokenManagerConfiguration>
{
    /// <inheritdoc/>
    public TokenManagerConfigurationCommand() : base("TokenManager") { }
}
