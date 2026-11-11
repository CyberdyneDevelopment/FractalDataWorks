using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSet key-field child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetKeyField")]
public sealed class DataSetKeyFieldConfigurationCommand : ConfigurationCommandBase<DataSetKeyFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetKeyFieldConfigurationCommand"/> class.</summary>
    public DataSetKeyFieldConfigurationCommand() : base("DataSetKeyField") { }
}
