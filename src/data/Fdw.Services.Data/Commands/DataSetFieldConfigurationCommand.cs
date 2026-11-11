using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSet field child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetField")]
public sealed class DataSetFieldConfigurationCommand : ConfigurationCommandBase<DataSetFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetFieldConfigurationCommand"/> class.</summary>
    public DataSetFieldConfigurationCommand() : base("DataSetField") { }
}
