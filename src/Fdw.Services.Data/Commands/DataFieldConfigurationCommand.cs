using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSet field child configuration.</summary>
// Why: ContainerName is "DataSetField" — the C# type (DataFieldConfiguration) and the table (data.DataSetField)
// names diverge, so the keystone's robust descriptor match resolves the container via THIS command's
// ContainerName (not a {Type}→{Container} name convention). Create() returns a ConfigurationSaveCommand
// whose translator resolves the physical DataSetRowId FK by subquery from the logical DataSetId.
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetField")]
public sealed class DataFieldConfigurationCommand : ConfigurationCommandBase<DataFieldConfiguration>
{
    /// <inheritdoc/>
    public DataFieldConfigurationCommand() : base("DataSetField") { }
}
