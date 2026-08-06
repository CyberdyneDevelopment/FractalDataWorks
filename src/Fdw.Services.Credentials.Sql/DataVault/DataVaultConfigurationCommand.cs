using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.DataVault;

/// <summary>
/// ConfigurationCommands TypeOption for the DataVault domain. Produces IDataCommand
/// instances against the DataVault configuration table using the base class defaults.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "DataVault")]
public sealed class DataVaultConfigurationCommand : ConfigurationCommandBase<DataVaultConfiguration>
{
    /// <inheritdoc/>
    public DataVaultConfigurationCommand() : base("DataVault") { }
}
