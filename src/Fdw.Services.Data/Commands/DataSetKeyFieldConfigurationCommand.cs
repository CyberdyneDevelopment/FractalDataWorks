using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSet key-field child configuration.</summary>
// Why: ContainerName "DataSetKeyField" is the table; enables the keystone cascade to save/match this child.
// Create() returns a ConfigurationSaveCommand whose translator resolves the physical DataSetRowId FK.
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetKeyField")]
public sealed class DataSetKeyFieldConfigurationCommand : ConfigurationCommandBase<DataSetKeyFieldConfiguration>
{
    /// <inheritdoc/>
    public DataSetKeyFieldConfigurationCommand() : base("DataSetKeyField") { }
}
