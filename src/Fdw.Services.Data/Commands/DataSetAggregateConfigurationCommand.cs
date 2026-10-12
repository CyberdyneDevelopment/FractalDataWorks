using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSetAggregate child configuration.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetAggregate")]
public sealed class DataSetAggregateConfigurationCommand : ConfigurationCommandBase<DataSetAggregateConfiguration>
{
    /// <inheritdoc/>
    public DataSetAggregateConfigurationCommand() : base("DataSetAggregate") { }
}
