using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.DataVault;

/// <summary>
/// ConfigurationCommands TypeOption for the DefaultDataVault typed body.
/// Produces IDataCommand instances against the DefaultDataVault configuration table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "DefaultDataVault")]
public sealed class DefaultDataVaultConfigurationCommand : ConfigurationCommandBase<DefaultDataVaultConfiguration>
{
    /// <inheritdoc/>
    public DefaultDataVaultConfigurationCommand() : base("DefaultDataVault") { }
}
