using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes <c>transform.AggregateTransformationConfigurationAggregation</c> rows as <see cref="AggregateTransformationAggregationConfiguration"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "AggregateTransformationConfigurationAggregation")]
public sealed class AggregateTransformationAggregationConfigurationCommand : ConfigurationCommandBase<AggregateTransformationAggregationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AggregateTransformationAggregationConfigurationCommand"/> class.</summary>
    public AggregateTransformationAggregationConfigurationCommand() : base("AggregateTransformationConfigurationAggregation") { }
}
