using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>States that <see cref="AggregateTransformationAggregationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "AggregateTransformationAggregationConfiguration")]
public sealed class AggregateTransformationAggregationConfigurationNoValidation : UnvalidatedConfiguration<AggregateTransformationAggregationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AggregateTransformationAggregationConfigurationNoValidation"/> class.</summary>
    public AggregateTransformationAggregationConfigurationNoValidation()
        : base("AggregateTransformationAggregationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
