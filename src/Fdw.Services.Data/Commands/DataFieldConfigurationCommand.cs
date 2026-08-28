using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSet field child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetField")]
public sealed class DataFieldConfigurationCommand : ConfigurationCommandBase<DataFieldConfiguration>
{
    /// <inheritdoc/>
    public DataFieldConfigurationCommand() : base("DataSetField") { }
}
