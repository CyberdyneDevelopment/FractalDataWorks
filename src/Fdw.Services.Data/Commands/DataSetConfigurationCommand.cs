using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSet configuration domain.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSet")]
public sealed class DataSetConfigurationCommand : ConfigurationCommandBase<DataSetConfiguration>
{
    /// <inheritdoc/>
    public DataSetConfigurationCommand() : base("DataSet") { }
}
