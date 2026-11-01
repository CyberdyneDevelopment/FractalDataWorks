using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes <c>transform.AggregateTransformationConfigurationGroupByField</c> rows as <see cref="AggregateTransformationGroupByFieldConfiguration"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "AggregateTransformationConfigurationGroupByField")]
public sealed class AggregateTransformationGroupByFieldConfigurationCommand : ConfigurationCommandBase<AggregateTransformationGroupByFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AggregateTransformationGroupByFieldConfigurationCommand"/> class.</summary>
    public AggregateTransformationGroupByFieldConfigurationCommand() : base("AggregateTransformationConfigurationGroupByField") { }
}
