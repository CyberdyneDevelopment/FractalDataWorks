using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.TokenManagers.Commands;

/// <summary>ConfigurationCommands TypeOption for the TokenManager configuration domain (auth.TokenManager).</summary>
[TypeOption(typeof(ConfigurationCommands), "TokenManager")]
public sealed class TokenManagerConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TokenManagerConfigurationCommand"/> class.</summary>
    public TokenManagerConfigurationCommand() : base("TokenManager") { }
}
