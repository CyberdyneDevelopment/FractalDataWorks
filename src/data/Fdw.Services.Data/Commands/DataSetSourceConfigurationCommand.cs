using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSetSource child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetSource")]
public sealed class DataSetSourceConfigurationCommand : ConfigurationCommandBase<DataSetSourceConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetSourceConfigurationCommand"/> class.</summary>
    public DataSetSourceConfigurationCommand() : base("DataSetSource") { }
}
