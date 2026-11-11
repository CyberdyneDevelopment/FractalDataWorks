using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSetFilter child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetFilter")]
public sealed class DataSetFilterConfigurationCommand : ConfigurationCommandBase<DataSetFilterConditionConfiguration>
{
    /// <inheritdoc/>
    public DataSetFilterConfigurationCommand() : base("DataSetFilter") { }
}
