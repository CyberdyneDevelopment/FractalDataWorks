using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSet key-field child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetKeyField")]
public sealed class DataSetKeyFieldConfigurationCommand : ConfigurationCommandBase<DataSetKeyFieldConfiguration>
{
    /// <inheritdoc/>
    public DataSetKeyFieldConfigurationCommand() : base("DataSetKeyField") { }
}
