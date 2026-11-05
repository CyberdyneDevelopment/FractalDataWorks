using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>States that <see cref="AggregateTransformationGroupByFieldConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "AggregateTransformationGroupByFieldConfiguration")]
public sealed class AggregateTransformationGroupByFieldConfigurationNoValidation : UnvalidatedConfiguration<AggregateTransformationGroupByFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AggregateTransformationGroupByFieldConfigurationNoValidation"/> class.</summary>
    public AggregateTransformationGroupByFieldConfigurationNoValidation()
        : base("AggregateTransformationGroupByFieldConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
