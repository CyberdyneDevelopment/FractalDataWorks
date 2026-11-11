using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.DataVault;

/// <summary>
/// ConfigurationCommands TypeOption for the DefaultDataVault typed body.
/// Produces IDataCommand instances against the DefaultDataVault configuration table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "DefaultDataVaultConfiguration")]
public sealed class SqlDataVaultConfigurationCommand : ConfigurationCommandBase<SqlDataVaultConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlDataVaultConfigurationCommand"/> class.</summary>
    public SqlDataVaultConfigurationCommand() : base("DefaultDataVaultConfiguration") { }
}
