using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.DataVault;

/// <summary>
/// ConfigurationCommands TypeOption for the DataVault domain. Produces IDataCommand
/// instances against the DataVault configuration table using the base class defaults.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "DataVault")]
public sealed class DataVaultConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataVaultConfigurationCommand"/> class.</summary>
    public DataVaultConfigurationCommand() : base("DataVault") { }
}
