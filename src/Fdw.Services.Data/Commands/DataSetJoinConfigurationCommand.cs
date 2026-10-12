using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSetJoin child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetJoin")]
public sealed class DataSetJoinConfigurationCommand : ConfigurationCommandBase<DataSetJoinConfiguration>
{
    /// <inheritdoc/>
    public DataSetJoinConfigurationCommand() : base("DataSetJoin") { }
}
