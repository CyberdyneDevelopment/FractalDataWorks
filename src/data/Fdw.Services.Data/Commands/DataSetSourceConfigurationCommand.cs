using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSetSource child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetSource")]
public sealed class DataSetSourceConfigurationCommand : ConfigurationCommandBase<DataSetSourceConfiguration>
{
    /// <inheritdoc/>
    public DataSetSourceConfigurationCommand() : base("DataSetSource") { }
}
