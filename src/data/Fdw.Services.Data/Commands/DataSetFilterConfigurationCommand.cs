using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSetFilter child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetFilter")]
public sealed class DataSetFilterConfigurationCommand : ConfigurationCommandBase<DataSetFilterConditionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetFilterConfigurationCommand"/> class.</summary>
    public DataSetFilterConfigurationCommand() : base("DataSetFilter") { }
}
