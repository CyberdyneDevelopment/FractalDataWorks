using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSetSource child configuration.</summary>
// Why: the keystone save cascade resolves each child's command by ConfigType; ContainerName "DataSetSource"
// is the table. Create() returns a ConfigurationSaveCommand whose translator resolves the physical
// DataSetRowId FK by subquery from the logical DataSetId set by the cascade.
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetSource")]
public sealed class DataSetSourceConfigurationCommand : ConfigurationCommandBase<DataSetSourceConfiguration>
{
    /// <inheritdoc/>
    public DataSetSourceConfigurationCommand() : base("DataSetSource") { }
}
